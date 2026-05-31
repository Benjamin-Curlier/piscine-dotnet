# ex04-decomposer — God function → décomposition (bonus)

> Exercice **bonus** : un peu plus exigeant, non bloquant pour la suite.

## Objectif

Le code fourni entasse tout dans le flux principal : il **parse** l'entrée, **calcule** une
moyenne et **formate** la sortie, mélangés. Refactore-le en **trois méthodes** aux responsabilités
claires, sans changer le comportement.

Le programme lit une ligne `nom,note1,note2,...` et affiche `nom: moyenne` (moyenne **entière**).

Exemples : `Alice,12,15,9` → `Alice: 12` ; `Bob,20,10` → `Bob: 15`.

## Livrable

- `Bulletin.cs`

## Contraintes

- Comportement identique (tests `io` = filet de régression).
- Sépare au moins : `Parser` (nom + notes), `Moyenne` (calcul), `Formater` (chaîne finale).

## Indices

- `Parser` peut renvoyer un tuple `(string nom, int[] notes)`.
- La moyenne est une **division entière** (`somme / nombre`) — ne la transforme pas en flottant.
- Une fonction qui « fait tout » est difficile à tester et à faire évoluer ; une responsabilité
  par méthode = le principe de responsabilité unique.
