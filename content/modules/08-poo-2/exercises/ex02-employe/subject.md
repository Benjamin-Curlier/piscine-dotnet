# ex02-employe — Masse salariale

## Objectif

Lis trois entiers, un par ligne : le **salaire mensuel** d'un employé fixe, puis le **salaire de
base** et la **commission** d'un commercial. Affiche la **masse salariale** totale, c'est-à-dire la
somme des salaires mensuels des deux employés.

Le salaire mensuel d'un commercial vaut `base + commission`.

Exemple : `2000`, `1500`, `500` → `2000 + (1500 + 500)` = `4000`.

## Livrable

- `Employe.cs`

## Indices

- Déclare une classe **`abstract class Employe`** avec une méthode abstraite
  `public abstract int SalaireMensuel();` (sans corps).
- `EmployeFixe` renvoie son salaire ; `Commercial` renvoie `base + commission`. Chacune fait
  `override` de `SalaireMensuel()`.
- Range les deux employés dans une `List<Employe>`, additionne leurs `SalaireMensuel()`.
- `List<>` nécessite `using System.Collections.Generic;`.
