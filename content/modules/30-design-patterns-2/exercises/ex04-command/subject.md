# ex04-command — Command & undo (bonus)

> Exercice **bonus** : un peu plus exigeant, non bloquant pour la suite.

## Objectif

Implémente le patron **Command** : transformer des actions en **objets** que l'on peut exécuter,
empiler dans un historique, et **annuler**.

Lis des commandes ligne par ligne jusqu'à la fin de l'entrée, appliquées à une calculatrice qui
démarre à `0` :
- `add N` → ajoute `N` ;
- `sub N` → soustrait `N` ;
- `undo` → annule la dernière commande exécutée (rien si l'historique est vide).

Affiche la valeur finale.

Exemple :
```
add 5
add 3
undo
sub 2
```
→ `3` (0 → 5 → 8 → 5 → 3).

## Livrable

- `Commande.cs`

## Contraintes

- Chaque opération est un **objet commande** avec `Executer` **et** `Annuler`.
- L'undo s'appuie sur une **pile** d'historique — il ne recalcule rien à la main.

## Indices

- `interface ICommande { void Executer(Calculatrice c); void Annuler(Calculatrice c); }`.
- `Ajouter` fait `+N` à l'exécution et `-N` à l'annulation ; `Soustraire` l'inverse.
- À chaque commande exécutée, empile-la (`Stack<ICommande>`). Sur `undo`, dépile et appelle
  `Annuler`. Lis les lignes avec `while ((ligne = System.Console.ReadLine()) != null)`.
