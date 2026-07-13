using System;
using System.Net.Http;
using System.Text.Json;

// args[0] = URL de base du serveur (ex. "http://127.0.0.1:12345/").
// args[1] = chemin de la route à appeler (ex. "meteo/paris").
var baseUrl = args[0];
var path = args[1];

using var client = new HttpClient();

// TODO : appelle GET {baseUrl}{path} avec GetStringAsync et récupère le JSON.
// TODO : désérialise-le (JsonSerializer.Deserialize<Meteo>) puis affiche
//        « Ville : Temp°C (humidité X%) » avec System.Console.Write (sans saut de ligne).

// TODO : complète cette classe pour représenter la réponse JSON (ville, tempC, humidite).
sealed class Meteo
{
}
