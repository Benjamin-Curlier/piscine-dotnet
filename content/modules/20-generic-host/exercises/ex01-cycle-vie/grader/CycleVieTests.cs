using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

// Tests cachés (grader unit) : « Réussi » exige une vraie implémentation d'IHostedService dont
// StartAsync/StopAsync journalisent aux bons moments — pas deux Console.Write dans le bon ordre.
public class CycleVieTests
{
    [Fact]
    public void Cycle_ImplementeIHostedService()
    {
        Assert.True(
            typeof(IHostedService).IsAssignableFrom(typeof(Cycle)),
            "Cycle doit implémenter IHostedService.");
    }

    [Fact]
    public async Task StartAsync_Journalise_Demarrage_Et_DemandeLArret()
    {
        var logger = new LoggerEnregistreur<Cycle>();
        var cycle = new CycleFactice();
        var service = new Cycle(logger, cycle);

        await service.StartAsync(CancellationToken.None);

        Assert.Contains(logger.Entrees, e => e.Niveau == LogLevel.Information && e.Message == "Démarrage");
        Assert.True(
            cycle.ArretDemande,
            "StartAsync doit demander l'arrêt via IHostApplicationLifetime.StopApplication().");
    }

    [Fact]
    public async Task StopAsync_Journalise_Arret()
    {
        var logger = new LoggerEnregistreur<Cycle>();
        var service = new Cycle(logger, new CycleFactice());

        await service.StopAsync(CancellationToken.None);

        Assert.Contains(logger.Entrees, e => e.Niveau == LogLevel.Information && e.Message == "Arrêt");
    }
}

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

sealed class CycleFactice : IHostApplicationLifetime
{
    public bool ArretDemande { get; private set; }

    public CancellationToken ApplicationStarted => CancellationToken.None;

    public CancellationToken ApplicationStopping => CancellationToken.None;

    public CancellationToken ApplicationStopped => CancellationToken.None;

    public void StopApplication() => ArretDemande = true;
}
