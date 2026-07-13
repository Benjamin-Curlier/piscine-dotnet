using Microsoft.Extensions.Logging;

using var fabrique = Journalisation.CreerFabrique();

var app = fabrique.CreateLogger("App");
var db = fabrique.CreateLogger("Db");

app.LogInformation("Démarrage");
db.LogInformation("Requête SELECT *");   // filtré : Db n'émet qu'à partir de Warning
db.LogWarning("Requête lente (1.2s)");
app.LogInformation("Arrêt");

static class Journalisation
{
    // Un SEUL LoggerFactory : provider fourni, niveau minimum Information, et un filtre par catégorie
    // qui ne laisse la catégorie "Db" émettre qu'à partir de Warning.
    public static ILoggerFactory CreerFabrique()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new CaptureLoggerProvider());
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Db", LogLevel.Warning);
        });
    }
}
