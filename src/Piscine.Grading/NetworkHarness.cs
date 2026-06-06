using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Piscine.Grading;

/// <summary>
/// Serveur de test embarqué pour le grader <c>reseau</c> : un **écho TCP** déterministe sur loopback
/// (port éphémère). Renvoie chaque ligne reçue, ce qui permet une comparaison de sortie reproductible.
/// </summary>
public sealed class NetworkHarness : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;

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

                _ = Task.Run(() => ServeAsync(client));
            }
        }
        catch (Exception)
        {
            // Arrêt du serveur : on ignore.
        }
    }

    private static async Task ServeAsync(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\n" })
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) is not null)
                {
                    await writer.WriteLineAsync(line);
                }
            }
        }
        catch (Exception)
        {
            // Connexion fermée par le client : fin normale.
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

        try
        {
            _acceptLoop.Wait(TimeSpan.FromSeconds(1));
        }
        catch (Exception)
        {
            // boucle déjà terminée
        }

        _cts.Dispose();
    }
}
