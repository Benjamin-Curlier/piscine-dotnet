using Microsoft.Extensions.Logging;

// Lis QUATRE messages sur stdin, dans l'ordre :
//   1) message App (Information), 2) message Db (Information, sera filtré),
//   3) message Db (Warning), 4) message App (Information).
//
// Configure un seul LoggerFactory :
//   - provider fourni, niveau minimum Information ;
//   - un filtre par catégorie : builder.AddFilter("Db", LogLevel.Warning).
// Crée deux loggers : catégorie "App" et catégorie "Db". Émets, dans l'ordre :
//   app.LogInformation("{Message}", msgAppDemarrage);
//   db.LogInformation("{Message}", msgDbInfo);     // filtré (Db >= Warning)
//   db.LogWarning("{Message}", msgDbWarning);
//   app.LogInformation("{Message}", msgAppArret);

// À toi de jouer.
