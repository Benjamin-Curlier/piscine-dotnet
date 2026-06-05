using Microsoft.Extensions.Logging;

var id = int.Parse(System.Console.ReadLine()!);
var client = System.Console.ReadLine();

using var factory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new CaptureLoggerProvider());
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = factory.CreateLogger("Commandes");

logger.LogInformation("Commande {Id} validée pour {Client}", id, client);
