# ex02-chain-responsabilite — Pipeline de validation

## Mission

Les commandes d'Asteria doivent traverser plusieurs contrôles. Implémente une **Chain of
Responsibility** : chaque handler traite une règle ou transmet la requête au suivant.

L'entrée commence par `N`, puis contient `N` lignes `role montant`. Pour chaque ligne, affiche :

- `REFUS authentification` si le rôle n'est ni `operateur` ni `admin` ;
- `REFUS montant` si le montant est inférieur ou égal à zéro ;
- `REFUS limite` si un opérateur demande plus de 100 ;
- `ACCEPTE` sinon.

Les rôles sont insensibles à la casse.

## Livrable

- `Validation.cs`

## Contraintes

- Crée un `Handler` abstrait doté de `SetNext` et `Handle`.
- Répartis les règles entre au moins trois handlers concrets.
- Un handler satisfait par sa règle délègue au suivant ; il ne connaît pas les autres règles.

## Exemple

Entrée :

```text
3
visiteur 20
operateur 150
admin 150
```

Sortie :

```text
REFUS authentification
REFUS limite
ACCEPTE
```
