using System.Net.Http;

// args[0] contient l'URL de base du serveur de test (ex. "http://127.0.0.1:12345/").
// args[1] contient le chemin de la route à appeler (ex. "api/bonjour").
var baseUrl = args[0];
var path = args[1];

// Crée un HttpClient unique et réutilisable.
using var client = new HttpClient();

// TODO : appelle GET {baseUrl}{path} et affiche le corps de la réponse.
