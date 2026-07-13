# ex01-http-meteo — Lire une API météo JSON

## Objectif

Les vraies API web ne renvoient presque jamais un texte tout prêt : elles renvoient des **données**
en **JSON** (rappel module 16), à toi de les **exploiter**. Ici, tu appelles une route qui décrit la
météo d'une ville, tu **désérialises** le JSON, et tu affiches une **synthèse formatée** — pas le
corps brut.

## Convention d'appel

| Index     | Valeur             | Exemple                    |
|-----------|--------------------|----------------------------|
| `args[0]` | URL de base        | `http://127.0.0.1:12345/`  |
| `args[1]` | Chemin de la route | `meteo/paris`              |

La route à appeler est `{args[0]}{args[1]}`. Elle renvoie un objet JSON de la forme :

```json
{ "ville": "Paris", "tempC": 19, "humidite": 72 }
```

## Sortie attendue

Une seule ligne (sans saut de ligne final), au format exact :

```
Paris : 19°C (humidité 72%)
```

Pour `meteo/oslo` (température négative) :

```
Oslo : -4°C (humidité 88%)
```

## Livrable

- `Meteo.cs`

## Indices

- Récupère le corps avec `await client.GetStringAsync(url)` (un seul `HttpClient`, libéré par `using`).
- Désérialise avec `System.Text.Json` : `JsonSerializer.Deserialize<T>(json, options)`. Déclare une
  petite classe `Meteo { Ville, TempC, Humidite }` et active
  `new JsonSerializerOptions { PropertyNameCaseInsensitive = true }` pour faire correspondre les noms.
- Formate ensuite la chaîne avec `System.Console.Write(...)` (pas de `WriteLine` : aucune ligne en trop).
- Tu peux aussi utiliser `GetFromJsonAsync<T>` (`using System.Net.Http.Json;`) — au choix.
- Relis la section 4 du cours ([HttpClient](cours.md#httpclient-get)) et le module 16 pour le JSON.
