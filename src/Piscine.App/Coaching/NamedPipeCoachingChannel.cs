using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Piscine.App.Coaching;

/// <summary>
/// Recepteur named pipe : ecoute les evenements du shim <c>git</c> (une ligne JSON par commande) et
/// les republie via <see cref="CommandReceived"/>. Une connexion = une commande ; on re-ecoute apres
/// chaque message. Robuste : une connexion corrompue (IO/JSON) n'arrete pas la boucle.
/// </summary>
public sealed class NamedPipeCoachingChannel : ICoachingChannel
{
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
                        var line = await reader.ReadLineAsync(ct);
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
