using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Piscine.Grading;

/// <summary>
/// Serveur de test embarqué pour le grader <c>reseau</c> : un **écho TCP** déterministe sur loopback
/// (port éphémère). Renvoie chaque ligne reçue, ce qui permet une comparaison de sortie reproductible.
/// Les connexions en cours sont suivies et fermées de façon déterministe à <see cref="Dispose"/>.
/// </summary>
public sealed class NetworkHarness : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private readonly object _gate = new();
    private readonly List<Task> _connections = new();

    private NetworkHarness(TcpListener listener)
    {
        _listener = listener;
        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    /// <summary>Hôte du serveur de test (loopback).</summary>
    public string Host => "127.0.0.1";

    /// <summary>Port éphémère effectivement attribué.</summary>
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>Démarre un serveur d'écho TCP sur un port éphémère de loopback.</summary>
    public static NetworkHarness StartEcho()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new NetworkHarness(listener);
    }

    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var served = Task.Run(() => ServeAsync(client, _cts.Token));
                lock (_gate)
                {
                    _connections.Add(served);
                }
            }
        }
        catch (Exception)
        {
            // Arrêt du serveur : on ignore.
        }
    }

    private static async Task ServeAsync(TcpClient client, CancellationToken token)
    {
        // Le catch est *porteur* : il garantit qu'aucune exception ne remonte en tâche non observée.
        // L'enregistrement force la fermeture de la connexion si le harnais est libéré (candidat figé).
        using var registration = token.Register(
            static state =>
            {
                try
                {
                    ((TcpClient)state!).Close();
                }
                catch (Exception)
                {
                    // déjà fermée
                }
            },
            client);

        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\n" };

                string? line;
                while ((line = await reader.ReadLineAsync(token)) is not null)
                {
                    await writer.WriteLineAsync(line.AsMemory(), token);
                }
            }
        }
        catch (Exception)
        {
            // Connexion fermée (par le client ou par l'arrêt du harnais) : fin normale.
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listener.Stop();
        }
        catch (Exception)
        {
            // déjà arrêté
        }

        Task[] pending;
        lock (_gate)
        {
            pending = _connections.Append(_acceptLoop).ToArray();
        }

        try
        {
            Task.WaitAll(pending, TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            // tâches annulées/terminées
        }

        _cts.Dispose();
    }
}
