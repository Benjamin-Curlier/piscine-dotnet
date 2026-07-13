# ex02-echo-tcp — Client d'écho TCP

## Objectif

Descends d'un cran sous HTTP : parle **directement en TCP**. Un **serveur d'écho** t'attend sur
`127.0.0.1` (loopback). Un serveur d'écho renvoie **exactement** ce qu'on lui envoie. Ton programme
est le **client** : il lit des lignes sur l'**entrée standard**, les envoie au serveur, et **affiche
l'écho** de chacune.

## Convention d'appel

| Index     | Valeur | Exemple      |
|-----------|--------|--------------|
| `args[0]` | Hôte   | `127.0.0.1`  |
| `args[1]` | Port   | `54321`      |

> Le **port change à chaque exécution** (il est attribué dynamiquement). Impossible de le deviner :
> lis-le dans `args[1]` et connecte-toi vraiment au serveur.

## Comportement

Pour **chaque ligne** lue sur l'entrée standard, jusqu'à la fin de l'entrée :

1. envoie la ligne au serveur,
2. lis la ligne que le serveur te renvoie (l'écho),
3. affiche-la (une ligne de sortie par ligne d'entrée).

### Exemple

Entrée :

```
un
deux
trois
```

Sortie :

```
un
deux
trois
```

## Livrable

- `EchoClient.cs`

## Indices

- Connecte-toi avec `using var client = new TcpClient();` puis
  `await client.ConnectAsync(args[0], int.Parse(args[1]));`.
- Récupère le flux : `using var flux = client.GetStream();`. Emballe-le dans un `StreamReader` (lecture)
  et un `StreamWriter` (écriture) — mets `AutoFlush = true` et `NewLine = "\n"` sur l'écrivain pour que
  chaque ligne parte tout de suite.
- Boucle : `while ((ligne = Console.ReadLine()) is not null)` — envoie (`WriteLineAsync`), lis l'écho
  (`ReadLineAsync`), affiche (`Console.WriteLine`).
- Un appel réseau est **lent** : tout s'écrit en `async`/`await` (module 12).
- Relis la section 2 du cours ([Sockets TCP](cours.md#sockets-tcp)).
