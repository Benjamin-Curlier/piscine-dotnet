using Microsoft.Extensions.Logging;

var message = System.Console.ReadLine();

using var factory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new CaptureLoggerProvider());
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = factory.CreateLogger("App");
logger.LogInformation(message);
