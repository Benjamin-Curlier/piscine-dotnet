using Microsoft.Extensions.Logging;

// Le Main (création des loggers "App"/"Db" et émission des logs) t'est fourni. À toi de renseigner
// Journalisation.CreerFabrique() : un SEUL LoggerFactory, provider fourni, niveau minimum Information,
// et un filtre par catégorie pour que "Db" n'émette qu'à partir de Warning.

using var fabrique = Journalisation.CreerFabrique();

var app = fabrique.CreateLogger("App");
var db = fabrique.CreateLogger("Db");

app.LogInformation("Démarrage");
db.LogInformation("Requête SELECT *");   // doit être filtré (Db >= Warning)
db.LogWarning("Requête lente (1.2s)");
app.LogInformation("Arrêt");

static class Journalisation
{
    public static ILoggerFactory CreerFabrique()
    {
        return LoggerFactory.Create(builder =>
        {
            // TODO : builder.AddProvider(new CaptureLoggerProvider());
            //        builder.SetMinimumLevel(LogLevel.Information);
            //        builder.AddFilter("Db", LogLevel.Warning);
        });
    }
}
