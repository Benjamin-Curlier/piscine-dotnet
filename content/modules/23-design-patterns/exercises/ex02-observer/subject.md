# ex02-observer — Diffusion de messages

## Objectif

Lis un entier **N**, puis **N** messages (une ligne chacun). Un **sujet** diffuse chaque message à
deux **observateurs** abonnés : le premier préfixe le message par `[A] `, le second par `[B] `.
Pour chaque message, le sujet notifie **A puis B** (ordre déterministe).

Exemple : `2` puis `hello`, `bye` →
`[A] hello`, `[B] hello`, `[A] bye`, `[B] bye`.

## Livrable

- `Observer.cs`

## Indices

- Déclare une interface `IObservateur` avec `void Notifier(string message)`.
- Crée deux observateurs concrets : l'un affiche `[A] ` + message, l'autre `[B] ` + message.
- Le `Sujet` garde une `List<IObservateur>` ; sa méthode `Diffuser(string)` parcourt la liste et
  appelle `Notifier` sur chaque observateur **dans l'ordre d'ajout**. Abonne A avant B.
- C'est le patron **Observer** : le sujet ne connaît que le contrat `IObservateur`, pas les détails.
- `List<>` nécessite `using System.Collections.Generic;`. `int.Parse(System.Console.ReadLine())` lit un entier.
