using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Piscine.Core.Model;

namespace Piscine.Grading;

/// <summary>
/// Serveur de test embarqué pour le grader <c>reseau</c>. Deux modes :
/// <list type="bullet">
///   <item><see cref="StartEcho"/> — écho TCP déterministe sur loopback (port éphémère) ;</item>
///   <item><see cref="StartHttp"/> — serveur HTTP (<c>HttpListener</c>) sur loopback, routes
///     configurables (méthode + chemin → statut + corps).</item>
/// </list>
/// Toutes les connexions actives sont fermées de façon déterministe à <see cref="Dispose"/>.
/// </summary>
public sealed class NetworkHarness : IDisposable
{
    // ── TCP echo ─────────────────────────────────────────────────────────────

    private readonly TcpListener? _tcpListener;
    private readonly Task? _tcpAcceptLoop;
    private readonly object _gate = new();
    private readonly List<Task> _connections = new();

    // ── HTTP ─────────────────────────────────────────────────────────────────

    private readonly HttpListener? _httpListener;
    private readonly Task? _httpServeLoop;

    // ── shared ───────────────────────────────────────────────────────────────

    private readonly CancellationTokenSource _cts = new();
    private readonly int _port;

    private NetworkHarness(TcpListener tcpListener)
    {
        _tcpListener = tcpListener;
        _port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        _tcpAcceptLoop = Task.Run(TcpAcceptLoopAsync);
    }

    private NetworkHarness(HttpListener httpListener, int port, IReadOnlyList<HttpRouteConfig> routes)
    {
        _httpListener = httpListener;
        _port = port;
        _httpServeLoop = Task.Run(() => HttpServeLoopAsync(routes));
    }

    /// <summary>Hôte du serveur de test (loopback).</summary>
    public string Host => "127.0.0.1";

    /// <summary>Port éphémère effectivement attribué.</summary>
    public int Port => _port;

    /// <summary>URL de base du serveur HTTP (ex. <c>http://127.0.0.1:12345/</c>). Null si mode TCP.</summary>
    public string? BaseUrl => _httpListener is not null ? $"http://{Host}:{Port}/" : null;

    // ── factories ─────────────────────────────────────────────────────────────

    /// <summary>Démarre un serveur d'écho TCP sur un port éphémère de loopback.</summary>
    public static NetworkHarness StartEcho()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return new NetworkHarness(listener);
    }

    /// <summary>
    /// Démarre un serveur HTTP sur un port éphémère de loopback. Chaque route répond selon la
    /// configuration ; les routes inconnues reçoivent un 404. Le programme recrue reçoit l'URL de
    /// base (<c>http://127.0.0.1:{port}/</c>) comme premier argument.
    /// </summary>
    public static NetworkHarness StartHttp(IReadOnlyList<HttpRouteConfig> routes)
    {
        // HttpListener n'accepte pas le port 0 : on réserve un port libre via TcpListener. Entre la
        // fermeture de la sonde (GetFreePort) et HttpListener.Start(), un autre processus peut reprendre
        // le port (TOCTOU) → Start() lève alors HttpListenerException. On retente avec un nouveau port
        // libre plutôt que de laisser échouer la correction sur une course transitoire.
        const int maxAttempts = 5;
        Exception? lastError = null;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var port = GetFreePort();
            var prefix = $"http://127.0.0.1:{port}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            try
            {
                listener.Start();
            }
            catch (Exception ex) when (ex is HttpListenerException or SocketException)
            {
                // Port repris entre-temps : on ferme le listener non démarré et on retente.
                lastError = ex;
                listener.Close();
                continue;
            }

            return new NetworkHarness(listener, port, routes);
        }

        // Ports de loopback durablement indisponibles : on remonte (fail-closed). ExerciseGrader
        // convertit toute exception d'un grader en échec interne affiché mais non persisté (M-10).
        throw new InvalidOperationException(
            $"Impossible d'ouvrir un port HTTP de loopback après {maxAttempts} tentatives.", lastError);
    }

    private static int GetFreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    // ── TCP echo implementation ───────────────────────────────────────────────

    private async Task TcpAcceptLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _tcpListener!.AcceptTcpClientAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                var served = Task.Run(() => TcpServeAsync(client, _cts.Token));
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

    private static async Task TcpServeAsync(TcpClient client, CancellationToken token)
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

    // ── HTTP implementation ───────────────────────────────────────────────────

    private async Task HttpServeLoopAsync(IReadOnlyList<HttpRouteConfig> routes)
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _httpListener!.GetContextAsync();
                }
                catch (Exception)
                {
                    // Arrêt du listener ou annulation.
                    break;
                }

                // Traite chaque requête dans un thread dédié (non bloquant pour la boucle).
                _ = Task.Run(() => HandleHttpRequest(ctx, routes));
            }
        }
        catch (Exception)
        {
            // Arrêt du serveur : on ignore.
        }
    }

    private static void HandleHttpRequest(HttpListenerContext ctx, IReadOnlyList<HttpRouteConfig> routes)
    {
        try
        {
            var method = ctx.Request.HttpMethod;
            // Chemin sans slash de fin (ex. "/api/message") ; "/" reste "/".
            var path = ctx.Request.Url?.AbsolutePath.TrimEnd('/') ?? "/";
            if (path.Length == 0)
            {
                path = "/";
            }

            HttpRouteConfig? match = null;
            foreach (var route in routes)
            {
                if (string.Equals(route.Method, method, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(route.Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    match = route;
                    break;
                }
            }

            if (match is null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
                return;
            }

            ctx.Response.StatusCode = match.StatusCode;
            ctx.Response.ContentType = match.ContentType;
            var body = Encoding.UTF8.GetBytes(match.ResponseBody);
            ctx.Response.ContentLength64 = body.Length;
            ctx.Response.OutputStream.Write(body, 0, body.Length);
            ctx.Response.Close();
        }
        catch (Exception)
        {
            // Connexion interrompue (candidat figé ou fin de test) : fin normale.
            try
            {
                ctx.Response.Abort();
            }
            catch (Exception)
            {
                // déjà fermée
            }
        }
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _cts.Cancel();

        // Arrêt TCP
        if (_tcpListener is not null)
        {
            try
            {
                _tcpListener.Stop();
            }
            catch (Exception)
            {
                // déjà arrêté
            }
        }

        // Arrêt HTTP
        if (_httpListener is not null)
        {
            try
            {
                _httpListener.Stop();
            }
            catch (Exception)
            {
                // déjà arrêté
            }
        }

        var pending = new List<Task>();
        lock (_gate)
        {
            pending.AddRange(_connections);
        }

        if (_tcpAcceptLoop is not null)
        {
            pending.Add(_tcpAcceptLoop);
        }

        if (_httpServeLoop is not null)
        {
            pending.Add(_httpServeLoop);
        }

        try
        {
            Task.WaitAll(pending.ToArray(), TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            // tâches annulées/terminées
        }

        _cts.Dispose();
    }
}
