using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Lis n sur l'entrée AVANT de construire l'hôte, enregistre-le par DI, puis
// dans un BackgroundService calcule la somme 1..n, logue-la et arrête l'hôte.

var n = int.Parse(System.Console.ReadLine()!);

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CaptureLoggerProvider());
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.None);

builder.Services.AddSingleton(new Parametres(n));
builder.Services.AddHostedService<Travailleur>();

var host = builder.Build();
host.Run();

sealed record Parametres(int N);

sealed class Travailleur : BackgroundService
{
    private readonly ILogger<Travailleur> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Parametres _parametres;

    public Travailleur(ILogger<Travailleur> logger, IHostApplicationLifetime lifetime, Parametres parametres)
    {
        _logger = logger;
        _lifetime = lifetime;
        _parametres = parametres;
    }

    protected override System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
    {
        // TODO : calcule la somme 1.._parametres.N, logue
        // "Somme 1..{N} = {Somme}", puis _lifetime.StopApplication().
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
