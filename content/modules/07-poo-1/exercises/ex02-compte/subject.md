# ex02-compte — Compte bancaire

## Objectif

Lis trois entiers (un par ligne) : un **solde initial**, un **dépôt**, puis un **retrait**. Effectue
le dépôt puis le retrait sur un objet `CompteBancaire`, et affiche le **solde final**.

**Règle d'encapsulation** : un retrait n'est possible **que si** le montant ne dépasse pas le solde
(sinon il est ignoré).

Exemples :
- `100` / `50` / `30` → `120` (100 + 50 − 30)
- `100` / `0` / `200` → `100` (retrait refusé : 200 > 100)
- `0` / `100` / `100` → `0`

## Livrable

- `Compte.cs`

## Indices

- Champ `private int _solde;` et un **constructeur** `CompteBancaire(int soldeInitial)`.
- `Deposer(int montant)` ajoute ; `Retirer(int montant)` ne soustrait **que si** `montant <= _solde`.
- Expose le solde en **lecture seule** : `public int Solde => _solde;`.
