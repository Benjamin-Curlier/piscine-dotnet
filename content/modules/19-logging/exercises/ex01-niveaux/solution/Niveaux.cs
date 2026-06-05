using Microsoft.Extensions.Logging;

var nom = System.Console.ReadLine();

using var factory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new CaptureLoggerProvider());
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = factory.CreateLogger("App");

logger.LogDebug("Trace interne détaillée");
logger.LogInformation($"{nom} prêt");
logger.LogWarning("Mémoire faible");
logger.LogError("Échec du traitement");
