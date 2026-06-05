using Microsoft.Extensions.Logging;

// Lis un nom de composant sur l'entrée, configure un logger (catégorie "App")
// avec un niveau minimum Information, puis logue, dans l'ordre :
//   - Debug   : "Trace interne détaillée"   (ne doit PAS apparaître)
//   - Information : "{nom} prêt"
//   - Warning : "Mémoire faible"
//   - Error   : "Échec du traitement"

var nom = System.Console.ReadLine();

// À toi de jouer.
