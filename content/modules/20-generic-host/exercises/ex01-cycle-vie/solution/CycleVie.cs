using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Le nom du service est lu sur stdin, puis injecté dans le service hébergé via le conteneur DI.
var nom = System.Console.ReadLine() ?? string.Empty;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CaptureLoggerProvider());
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.None);

builder.Services.AddSingleton(new Etiquette(nom));
builder.Services.AddHostedService<Cycle>();

var host = builder.Build();
host.Run();

sealed record Etiquette(string Nom);

sealed class Cycle : IHostedService
{
    private readonly ILogger<Cycle> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Etiquette _etiquette;

    public Cycle(ILogger<Cycle> logger, IHostApplicationLifetime lifetime, Etiquette etiquette)
    {
        _logger = logger;
        _lifetime = lifetime;
        _etiquette = etiquette;
    }

    public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken)
    {
        _logger.LogInformation("Démarrage {Nom}", _etiquette.Nom);
        _lifetime.StopApplication();
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken)
    {
        _logger.LogInformation("Arrêt {Nom}", _etiquette.Nom);
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
