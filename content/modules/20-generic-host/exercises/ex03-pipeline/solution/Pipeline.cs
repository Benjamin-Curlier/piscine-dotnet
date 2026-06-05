using System.Linq;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        var channel = Channel.CreateUnbounded<int>();

        foreach (var valeur in _entree.Nombres)
        {
            await channel.Writer.WriteAsync(valeur, stoppingToken);
        }

        channel.Writer.Complete();

        var total = 0;
        await foreach (var valeur in channel.Reader.ReadAllAsync(stoppingToken))
        {
            var doubleValeur = valeur * 2;
            total += doubleValeur;
            _logger.LogInformation("Traité {Valeur} -> {Double}", valeur, doubleValeur);
        }

        _logger.LogInformation("Total = {Total}", total);
        _lifetime.StopApplication();
    }
}
