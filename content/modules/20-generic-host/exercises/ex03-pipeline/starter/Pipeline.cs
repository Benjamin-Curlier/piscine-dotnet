using System.Linq;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Lis une ligne d'entiers séparés par des espaces, fais-les transiter par un
// Channel<int> à l'intérieur d'un service hébergé (producteur puis consommateur),
// logue chaque traitement et le total, puis arrête l'hôte.

var nombres = System.Console.ReadLine()!
    .Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
    .Select(int.Parse)
    .ToArray();

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CaptureLoggerProvider());
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.None);

builder.Services.AddSingleton(new Entree(nombres));
builder.Services.AddHostedService<Pipeline>();

var host = builder.Build();
host.Run();

sealed record Entree(int[] Nombres);

sealed class Pipeline : BackgroundService
{
    private readonly ILogger<Pipeline> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Entree _entree;

    public Pipeline(ILogger<Pipeline> logger, IHostApplicationLifetime lifetime, Entree entree)
    {
        _logger = logger;
        _lifetime = lifetime;
        _entree = entree;
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
    {
        // TODO :
        // 1. Channel.CreateUnbounded<int>() ; écris toutes les valeurs puis Writer.Complete().
        // 2. Consomme via Reader.ReadAllAsync : logue "Traité {Valeur} -> {Double}" (le double),
        //    en accumulant le total.
        // 3. Logue "Total = {Total}", puis _lifetime.StopApplication().
        await System.Threading.Tasks.Task.CompletedTask;
    }
}
