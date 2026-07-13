using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Construis un Generic Host qui enregistre un service hébergé `Worker`.
// La configuration du logging, la lecture de la consigne (sur stdin) et son enregistrement
// dans le conteneur DI sont déjà écrits ci-dessous : complète le Worker.

var tache = System.Console.ReadLine() ?? string.Empty;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CaptureLoggerProvider());
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.None);

builder.Services.AddSingleton(new Consigne(tache));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

sealed record Consigne(string Tache);

sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Consigne _consigne;

    public Worker(ILogger<Worker> logger, IHostApplicationLifetime lifetime, Consigne consigne)
    {
        _logger = logger;
        _lifetime = lifetime;
        _consigne = consigne;
    }

    protected override System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
    {
        // TODO : logue "Hôte démarré, travail effectué : {Tache}" en Information
        //        (logging structuré avec _consigne.Tache), puis demande l'arrêt
        //        avec _lifetime.StopApplication().
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
