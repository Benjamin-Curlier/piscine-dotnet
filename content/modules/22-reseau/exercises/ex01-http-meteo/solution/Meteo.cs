using System;
using System.Net.Http;
using System.Text.Json;

var baseUrl = args[0];
var path = args[1];

// Un seul HttpClient, libéré à la fin du programme.
using var client = new HttpClient();

// On récupère le corps de la réponse (du JSON) sous forme de chaîne.
var json = await client.GetStringAsync(baseUrl + path);

// On désérialise le JSON vers un objet C#. Les noms JSON (ville, tempC, humidite) sont en casse
// différente des propriétés : PropertyNameCaseInsensitive les fait correspondre.
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var meteo = JsonSerializer.Deserialize<Meteo>(json, options)!;

// On affiche une synthèse formatée (pas le corps brut).
Console.Write($"{meteo.Ville} : {meteo.TempC}°C (humidité {meteo.Humidite}%)");

// Représentation C# de la réponse JSON.
sealed class Meteo
{
    public string Ville { get; set; } = string.Empty;

    public int TempC { get; set; }

    public int Humidite { get; set; }
}
