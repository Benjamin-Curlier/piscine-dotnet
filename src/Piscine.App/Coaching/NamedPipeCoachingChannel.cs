using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Piscine.App.Coaching;

/// <summary>
/// Recepteur named pipe : ecoute les evenements du shim <c>git</c> (une ligne JSON par commande) et
/// les republie via <see cref="CommandReceived"/>. Une connexion = une commande ; on re-ecoute apres
/// chaque message. Robuste : une connexion corrompue (IO/JSON) n'arrete pas la boucle. La lecture est
/// <b>bornee</b> (<see cref="MaxMessageChars"/>) : un client local hostile (la recrue connait le nom du
/// pipe via son env) ne peut pas faire croitre le buffer sans borne (OOM) en envoyant une ligne geante
/// sans <c>\n</c> ; au-dela du plafond, la connexion est abandonnee sans deserialisation.
/// </summary>
public sealed class NamedPipeCoachingChannel : ICoachingChannel
{
    // Plafond de lecture d'une ligne (en caracteres). Un evenement du shim (argv + code + cwd) tient
    // tres largement en deca ; on borne pour ne jamais accumuler sans limite face a un client hostile.
    private const int MaxMessageChars = 64 * 1024;

    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public string Endpoint { get; } = $"piscine-coach-{Guid.NewGuid():N}";

    public event Action<GitCommandEvent>? CommandReceived;

    public void Start()
    {
        if (_loop is not null)
        {
            return;
        }

        // On cree le PREMIER pipe serveur de facon SYNCHRONE : des que Start() retourne, le canal est
        // garanti a l'ecoute (le shim a un timeout de connexion court — on evite toute course au
        // demarrage de la session).
        var first = CreateServer();
        _loop = Task.Run(() => AcceptLoopAsync(first, _cts.Token));
    }

    private NamedPipeServerStream CreateServer() =>
        new(
            Endpoint,
            PipeDirection.In,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

    private async Task AcceptLoopAsync(NamedPipeServerStream first, CancellationToken ct)
    {
        var server = first;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await server.WaitForConnectionAsync(ct);

                    // leaveOpen: true → le pipe servi appartient à CE scope, pas au StreamReader : on le
                    // dispose explicitement ci-dessous, sur TOUS les chemins (y compris une erreur de
                    // WaitForConnectionAsync, qui survient avant la creation du reader — sinon fuite de
                    // handle de pipe sur une longue session).
                    using (var reader = new StreamReader(server, leaveOpen: true))
                    {
                        var line = await ReadBoundedLineAsync(reader, ct);
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var payload = JsonSerializer.Deserialize(line, ChannelJsonContext.Default.ChannelPayload);
                            if (payload is not null)
                            {
                                var evt = new GitCommandEvent(
                                    payload.Argv ?? [],
                                    payload.ExitCode,
                                    payload.Cwd ?? string.Empty);
                                CommandReceived?.Invoke(evt);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Arret demande (Dispose) : on sort proprement (le finally dispose le serveur courant).
                    return;
                }
                catch (Exception e) when (e is IOException or JsonException)
                {
                    // Connexion corrompue / JSON invalide : on re-ecoute sans tuer la boucle.
                }

                // Fin de connexion (succes OU erreur) : on dispose le pipe servi et on en recree un pour
                // la connexion suivante.
                await server.DisposeAsync();
                server = CreateServer();
            }
        }
        finally
        {
            // Couvre l'annulation entre deux iterations (serveur fraichement recree, jamais servi).
            await server.DisposeAsync();
        }
    }

    /// <summary>
    /// Lit une ligne (jusqu'au premier <c>\n</c>) en <b>bornant</b> la taille accumulee a
    /// <see cref="MaxMessageChars"/>. Renvoie la ligne sans son terminateur, ou <c>null</c> si le flux
    /// se termine sans contenu OU si le plafond est franchi avant tout <c>\n</c> (client hostile) — dans
    /// ce dernier cas on abandonne la lecture immediatement, sans accumuler le reste ni deserialiser.
    /// </summary>
    private static async Task<string?> ReadBoundedLineAsync(StreamReader reader, CancellationToken ct)
    {
        var builder = new StringBuilder();
        var buffer = new char[4096];

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(), ct);
            if (read == 0)
            {
                // Fin de flux sans '\n' final : on renvoie ce qu'on a (peut etre vide → null).
                return builder.Length == 0 ? null : builder.ToString();
            }

            for (var i = 0; i < read; i++)
            {
                var c = buffer[i];
                if (c == '\n')
                {
                    // Fin de ligne (une connexion = une commande) : on ignore un eventuel reste.
                    if (builder.Length > 0 && builder[^1] == '\r')
                    {
                        builder.Length--;
                    }

                    return builder.ToString();
                }

                builder.Append(c);
                if (builder.Length > MaxMessageChars)
                {
                    // Ligne geante sans '\n' (client local hostile) : on abandonne cette connexion sans
                    // deserialiser, plutot que de laisser le buffer croitre sans borne (OOM). La boucle
                    // appelante disposera le pipe et re-ecoutera.
                    return null;
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch (Exception e) when (e is OperationCanceledException or IOException)
            {
                // Boucle deja terminee/annulee.
            }
        }

        _cts.Dispose();
    }
}

/// <summary>DTO de transport (forme JSON du shim) — distinct du modele metier <see cref="GitCommandEvent"/>.</summary>
internal sealed record ChannelPayload(
    [property: JsonPropertyName("argv")] IReadOnlyList<string>? Argv,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("cwd")] string? Cwd);

[JsonSerializable(typeof(ChannelPayload))]
internal sealed partial class ChannelJsonContext : JsonSerializerContext;
