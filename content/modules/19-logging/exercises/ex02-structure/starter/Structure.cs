using Microsoft.Extensions.Logging;

// Lis un identifiant (entier) puis un nom de client. Avec un logger de catégorie
// "Commandes" (niveau minimum Information), émets UN log Information utilisant un
// message à trous nommés :
//   logger.LogInformation("Commande {Id} validée pour {Client}", id, client);

var id = int.Parse(System.Console.ReadLine()!);
var client = System.Console.ReadLine();

// À toi de jouer.
