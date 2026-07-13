using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Implémente IHostedService directement pour observer le cycle de vie :
//   - StartAsync : logue "Démarrage" (puis demande l'arrêt) ;
//   - StopAsync  : logue "Arrêt".

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CaptureLoggerProvider());
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.None);

builder.Services.AddHostedService<Cycle>();

var host = builder.Build();
host.Run();

sealed class Cycle : IHostedService
{
    private readonly ILogger<Cycle> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public Cycle(ILogger<Cycle> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken)
    {
        // TODO : logue "Démarrage", puis _lifetime.StopApplication().
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken)
    {
        // TODO : logue "Arrêt".
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
