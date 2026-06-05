using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Construis un Generic Host qui enregistre un service hébergé `Worker`.
// La configuration du logging (provider fourni + on tait les logs internes du host)
// est déjà écrite ci-dessous : complète le Worker.

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CaptureLoggerProvider());
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.None);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
    {
        // TODO : logue "Hôte démarré, travail effectué" en Information,
        // puis demande l'arrêt avec _lifetime.StopApplication().
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
