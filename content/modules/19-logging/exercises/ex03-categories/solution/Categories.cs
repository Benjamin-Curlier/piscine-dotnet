using Microsoft.Extensions.Logging;

// Quatre messages lus sur stdin, dans l'ordre : App (info), Db (info, filtré), Db (warning), App (info).
var appDemarrage = System.Console.ReadLine();
var dbInfo = System.Console.ReadLine();
var dbWarning = System.Console.ReadLine();
var appArret = System.Console.ReadLine();

using var factory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new CaptureLoggerProvider());
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddFilter("Db", LogLevel.Warning);
});

var app = factory.CreateLogger("App");
var db = factory.CreateLogger("Db");

app.LogInformation("{Message}", appDemarrage);
db.LogInformation("{Message}", dbInfo);       // filtré : Db n'émet qu'à partir de Warning
db.LogWarning("{Message}", dbWarning);
app.LogInformation("{Message}", appArret);
