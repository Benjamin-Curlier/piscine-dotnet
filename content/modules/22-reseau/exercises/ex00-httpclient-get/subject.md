# ex00-httpclient-get — Premier appel HTTP avec HttpClient

## Objectif

Écris un programme console qui reçoit une **URL de base** et un **chemin de route** en arguments,
effectue une requête **GET** et affiche le **corps de la réponse** sur la sortie standard.

## Convention d'appel

| Index    | Valeur            | Exemple                    |
|----------|-------------------|----------------------------|
| `args[0]`| URL de base       | `http://127.0.0.1:12345/`  |
| `args[1]`| Chemin de la route| `api/bonjour`              |

La route à appeler est `{args[0]}{args[1]}`.

## Livrable

- `HttpGet.cs`

## Indices

- Crée **un seul** `HttpClient` et libère-le avec `using`.
- `await client.GetStringAsync(url)` envoie un GET et renvoie le corps en `string`.
- Affiche avec `System.Console.Write(...)` (sans saut de ligne supplémentaire).
- Relis la section 4 du cours pour les détails de `HttpClient`.
