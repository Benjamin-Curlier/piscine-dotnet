using Microsoft.Extensions.Logging;

// Configure un seul LoggerFactory :
//   - provider fourni, niveau minimum Information ;
//   - un filtre par catégorie : builder.AddFilter("Db", LogLevel.Warning).
// Crée deux loggers : catégorie "App" et catégorie "Db". Émets, dans l'ordre :
//   app.LogInformation("Démarrage");
//   db.LogInformation("Requête SELECT *");     // filtré (Db >= Warning)
//   db.LogWarning("Requête lente (1.2s)");
//   app.LogInformation("Arrêt");

// À toi de jouer.
