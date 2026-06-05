using Microsoft.Extensions.Logging;

using var factory = LoggerFactory.Create(builder =>
{
    builder.AddProvider(new CaptureLoggerProvider());
    builder.SetMinimumLevel(LogLevel.Information);
    builder.AddFilter("Db", LogLevel.Warning);
});

var app = factory.CreateLogger("App");
var db = factory.CreateLogger("Db");

app.LogInformation("Démarrage");
db.LogInformation("Requête SELECT *");
db.LogWarning("Requête lente (1.2s)");
app.LogInformation("Arrêt");
