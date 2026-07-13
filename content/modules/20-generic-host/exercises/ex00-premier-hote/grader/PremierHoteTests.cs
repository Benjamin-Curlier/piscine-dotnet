using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

// Tests cachés (grader unit) : « Réussi » exige un VRAI service hébergé qui journalise via ILogger puis
// demande l'arrêt via IHostApplicationLifetime — pas un simple Console.Write de la ligne attendue.
public class PremierHoteTests
{
    [Fact]
    public void Worker_EstUnBackgroundService()
    {
        Assert.True(
            typeof(BackgroundService).IsAssignableFrom(typeof(Worker)),
            "Worker doit hériter de BackgroundService.");
    }

    [Fact]
    public async Task Worker_JournaliseEnInformation_PuisDemandeLArret()
    {
        var logger = new LoggerEnregistreur<Worker>();
        var cycle = new CycleFactice();
        var worker = new Worker(logger, cycle);

        // ExecuteAsync est protégé ; on l'invoque directement pour un résultat déterministe
        // (StartAsync planifie le travail en arrière-plan sur le pool de threads).
        var executeAsync = typeof(Worker).GetMethod(
            "ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(executeAsync);
        await (Task)executeAsync.Invoke(worker, new object[] { CancellationToken.None });

        Assert.Contains(
            logger.Entrees,
            e => e.Niveau == LogLevel.Information && e.Message == "Hôte démarré, travail effectué");
        Assert.True(
            cycle.ArretDemande,
            "Le worker doit demander l'arrêt via IHostApplicationLifetime.StopApplication().");
    }
}

// Capture les entrées de log au lieu de les écrire : permet d'asserter le niveau et le message réels.
sealed class LoggerEnregistreur<T> : ILogger<T>
{
    public List<(LogLevel Niveau, string Message)> Entrees { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => Portee.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entrees.Add((logLevel, formatter(state, exception)));

    private sealed class Portee : IDisposable
    {
        public static readonly Portee Instance = new();

        public void Dispose() { }
    }
}

// Enregistre l'appel à StopApplication() sans réellement arrêter un hôte.
sealed class CycleFactice : IHostApplicationLifetime
{
    public bool ArretDemande { get; private set; }

    public CancellationToken ApplicationStarted => CancellationToken.None;

    public CancellationToken ApplicationStopping => CancellationToken.None;

    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication() => ArretDemande = true;
}
