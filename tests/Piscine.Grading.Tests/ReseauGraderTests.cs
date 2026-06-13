using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Piscine.Core.Model;
using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class ReseauGraderTests
{
    // ── programmes recrue ─────────────────────────────────────────────────────

    // Programme recrue TCP : se connecte au serveur de test (args[0]:args[1]), renvoie chaque ligne de
    // stdin et imprime l'écho reçu. Usings explicites (ImplicitUsings désactivé côté grader).
    private const string EchoClient = """
        using System.IO;
        using System.Net.Sockets;

        var host = args[0];
        var port = int.Parse(args[1]);
        using var client = new TcpClient(host, port);
        using var stream = client.GetStream();
        var writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\n" };
        var reader = new StreamReader(stream);
        string? ligne;
        while ((ligne = System.Console.ReadLine()) is not null)
        {
            writer.WriteLine(ligne);
            var echo = reader.ReadLine();
            System.Console.WriteLine($"Reçu : {echo}");
        }
        """;

    // Programme recrue HTTP : appelle GET /api/message sur le serveur de test (args[0] = baseUrl),
    // affiche le corps de la réponse.
    private const string HttpGetClient = """
        using System.Net.Http;

        var baseUrl = args[0];
        using var client = new System.Net.Http.HttpClient();
        var body = client.GetStringAsync(baseUrl + "api/message").GetAwaiter().GetResult();
        System.Console.Write(body);
        """;

    // Programme recrue HTTP POST : envoie POST /api/echo?msg=<stdin>, affiche le corps.
    private const string HttpPostClient = """
        using System.Net.Http;

        var baseUrl = args[0];
        var msg = System.Console.ReadLine();
        using var client = new System.Net.Http.HttpClient();
        using var content = new System.Net.Http.StringContent(msg ?? string.Empty);
        var resp = client.PostAsync(baseUrl + "api/echo", content).GetAwaiter().GetResult();
        var body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        System.Console.Write(body);
        """;

    // ── helpers ───────────────────────────────────────────────────────────────

    private static GradingContext Context(string source) =>
        new(new Dictionary<string, string> { ["Program.cs"] = source });

    private static GradingStep TcpStep(params (string Stdin, string Expect)[] cases)
    {
        var step = new GradingStep { Type = "reseau", Network = new NetworkConfig { Mode = "echo" } };
        foreach (var (stdin, expect) in cases)
        {
            step.Cases.Add(new IoCase { Stdin = stdin, ExpectStdout = expect, ExpectExit = 0 });
        }

        return step;
    }

    private static GradingStep HttpStep(IReadOnlyList<HttpRouteConfig> routes, params (string Stdin, string Expect)[] cases)
    {
        var step = new GradingStep { Type = "reseau", Network = new NetworkConfig { Mode = "http", Routes = new List<HttpRouteConfig>(routes) } };
        foreach (var (stdin, expect) in cases)
        {
            step.Cases.Add(new IoCase { Stdin = stdin, ExpectStdout = expect, ExpectExit = 0 });
        }

        return step;
    }

    // ── tests TCP echo (inchangés) ─────────────────────────────────────────────

    [Fact]
    public void Harness_EchoesLine()
    {
        using var harness = NetworkHarness.StartEcho();
        using var client = new TcpClient(harness.Host, harness.Port);
        using var stream = client.GetStream();
        var writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\n" };
        var reader = new StreamReader(stream);

        writer.WriteLine("ping");

        Assert.Equal("ping", reader.ReadLine());
    }

    [Fact]
    public void Grade_Reussi_WhenEchoMatches()
    {
        var result = new ReseauGrader().Grade(Context(EchoClient), TcpStep(("bonjour\n", "Reçu : bonjour\n")));

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_Reussi_WithMultipleLines()
    {
        var result = new ReseauGrader().Grade(
            Context(EchoClient),
            TcpStep(("un\ndeux\ntrois\n", "Reçu : un\nReçu : deux\nReçu : trois\n")));

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_ARevoir_WhenOutputDiffers()
    {
        var result = new ReseauGrader().Grade(Context(EchoClient), TcpStep(("bonjour\n", "Reçu : autre\n")));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.IoMismatch, result.Trigger);
    }

    [Fact]
    public void Grade_ARevoir_WhenProgramDoesNotCompile()
    {
        var result = new ReseauGrader().Grade(
            Context("var x = ;"),
            TcpStep(("x\n", "y\n")));

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.CompileError, result.Trigger);
    }

    [Fact]
    public void Grade_ContentError_WhenUnknownMode()
    {
        var step = new GradingStep { Type = "reseau", Network = new NetworkConfig { Mode = "ftp" } };
        step.Cases.Add(new IoCase { Stdin = "x\n", ExpectStdout = "y\n", ExpectExit = 0 });

        var result = new ReseauGrader().Grade(Context(EchoClient), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Grade_ContentError_WhenNoCases()
    {
        var step = new GradingStep { Type = "reseau", Network = new NetworkConfig { Mode = "echo" } }; // aucune case

        var result = new ReseauGrader().Grade(Context(EchoClient), step);

        // Fail-closed : sans cas, un rendu qui compile ne doit pas « réussir » par défaut.
        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }

    // ── tests HTTP harness direct ──────────────────────────────────────────────

    [Fact]
    public async Task HttpHarness_ServesConfiguredRoute()
    {
        var routes = new List<HttpRouteConfig>
        {
            new() { Method = "GET", Path = "/api/message", StatusCode = 200, ResponseBody = "bonjour" },
        };
        using var harness = NetworkHarness.StartHttp(routes);

        using var http = new HttpClient();
        var body = await http.GetStringAsync(harness.BaseUrl + "api/message");

        Assert.Equal("bonjour", body);
    }

    [Fact]
    public async Task HttpHarness_Returns404_ForUnknownRoute()
    {
        var routes = new List<HttpRouteConfig>
        {
            new() { Method = "GET", Path = "/api/message", StatusCode = 200, ResponseBody = "ok" },
        };
        using var harness = NetworkHarness.StartHttp(routes);

        using var http = new HttpClient();
        var response = await http.GetAsync(harness.BaseUrl + "autre");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void HttpHarness_ExposesBaseUrl()
    {
        using var harness = NetworkHarness.StartHttp(new List<HttpRouteConfig>());

        Assert.NotNull(harness.BaseUrl);
        Assert.StartsWith("http://127.0.0.1:", harness.BaseUrl, System.StringComparison.Ordinal);
        Assert.EndsWith("/", harness.BaseUrl, System.StringComparison.Ordinal);
    }

    [Fact]
    public void HttpHarness_EchoHarness_HasNullBaseUrl()
    {
        using var harness = NetworkHarness.StartEcho();

        Assert.Null(harness.BaseUrl);
    }

    // ── tests grader HTTP ──────────────────────────────────────────────────────

    [Fact]
    public void Grade_Http_Reussi_WhenGetMatches()
    {
        var routes = new List<HttpRouteConfig>
        {
            new() { Method = "GET", Path = "/api/message", StatusCode = 200, ResponseBody = "Bonjour HTTP" },
        };
        var step = HttpStep(routes, (string.Empty, "Bonjour HTTP"));

        var result = new ReseauGrader().Grade(Context(HttpGetClient), step);

        Assert.Equal(GraderStatus.Reussi, result.Status);
    }

    [Fact]
    public void Grade_Http_ARevoir_WhenOutputDiffers()
    {
        var routes = new List<HttpRouteConfig>
        {
            new() { Method = "GET", Path = "/api/message", StatusCode = 200, ResponseBody = "Bonjour HTTP" },
        };
        var step = HttpStep(routes, (string.Empty, "Autre réponse attendue"));

        var result = new ReseauGrader().Grade(Context(HttpGetClient), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.IoMismatch, result.Trigger);
    }

    [Fact]
    public void Grade_Http_ARevoir_WhenProgramDoesNotCompile()
    {
        var routes = new List<HttpRouteConfig>
        {
            new() { Method = "GET", Path = "/api/message", StatusCode = 200, ResponseBody = "ok" },
        };
        var step = HttpStep(routes, (string.Empty, "ok"));
        step.Network!.Mode = "http"; // déjà http mais on réassure

        var result = new ReseauGrader().Grade(Context("var x = ;"), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Equal(FeedbackTriggers.CompileError, result.Trigger);
    }

    [Fact]
    public void Grade_ContentError_WhenHttpModeHasNoRoutes()
    {
        var step = new GradingStep { Type = "reseau", Network = new NetworkConfig { Mode = "http" } };
        step.Cases.Add(new IoCase { Stdin = string.Empty, ExpectStdout = "ok", ExpectExit = 0 });

        var result = new ReseauGrader().Grade(Context(HttpGetClient), step);

        Assert.Equal(GraderStatus.ARevoir, result.Status);
        Assert.Contains(result.Messages, m => m.Contains("contenu", System.StringComparison.OrdinalIgnoreCase));
    }
}
