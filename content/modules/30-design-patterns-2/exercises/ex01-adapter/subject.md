# ex01-adapter — Adapter

## Objectif

Implémente le patron **Adapter** : faire collaborer un client avec une classe dont l'interface
ne correspond pas à ce qu'il attend, en intercalant un **adaptateur**.

Une classe « legacy » qu'on ne peut pas modifier expose tout un annuaire sous forme d'une seule
chaîne `"nom:age,nom:age,..."`. Le client, lui, veut juste appeler `Age(nom)`. Écris l'adaptateur.

Entrée :
- ligne 1 : la chaîne legacy (ex. `Alice:30,Bob:25`) ;
- ligne 2 : le nom recherché.

Affiche l'âge correspondant, ou `-1` si le nom est absent.

Exemple : `Alice:30,Bob:25` puis `Bob` → `25`.

## Livrable

- `Adaptateur.cs`

## Contraintes

- L'adaptateur **implémente** l'interface cible `IAnnuaire` et **délègue** à la classe legacy.
- Ne modifie pas la logique legacy : adapte-la.

## Indices

- `interface IAnnuaire { int Age(string nom); }`.
- `AnnuaireLegacy` détient la chaîne et expose `Tout()`.
- `AnnuaireAdapter` reçoit un `AnnuaireLegacy` au constructeur, découpe `Tout()` sur `,` puis sur
  `:`, et renvoie l'âge du bon nom (`-1` sinon).
