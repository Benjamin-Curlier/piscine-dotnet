# ex03-vehicule — Flotte de véhicules (bonus)

> Exercice **bonus** — difficulté **difficile**.

## Objectif

Modélise une classe **abstraite** `Vehicule` qui expose sa consommation (litres pour 100 km) et une
méthode `LitresPour(int km)` qui calcule le carburant nécessaire : `km * consommation / 100`.

Crée deux sous-classes : `Voiture` (**7 L/100 km**) et `Camion` (**25 L/100 km**).

Lis une **distance** en km (un multiple de 100), construis la flotte `{ Voiture, Camion }`, et
affiche la consommation de chaque véhicule (un entier par ligne), dans cet ordre.

Exemple : pour `200`, le programme affiche `14` (voiture) puis `50` (camion).

## Livrable

- `Vehicule.cs`

## Indices

- `abstract class Vehicule` avec une propriété abstraite (litres/100km) et la méthode concrète
  `LitresPour(int km) => km * Taux / 100;`.
- Chaque sous-classe `override` la propriété pour fournir son taux.
- Parcours une `List<Vehicule>` et appelle `LitresPour(km)` sur chaque élément : c'est le
  **polymorphisme** qui choisit la bonne consommation.
