using Microsoft.Extensions.Logging;

// Lis un message sur l'entrée standard, puis journalise-le en niveau Information
// via un logger de catégorie "App".
//
// 1. var factory = LoggerFactory.Create(builder => { ... }); pense à `using var`.
//    - builder.AddProvider(new CaptureLoggerProvider());
//    - builder.SetMinimumLevel(LogLevel.Information);
// 2. var logger = factory.CreateLogger("App");
// 3. logger.LogInformation(message);

var message = System.Console.ReadLine();

// À toi de jouer.
