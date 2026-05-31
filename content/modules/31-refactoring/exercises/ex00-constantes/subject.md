# ex00-constantes — Nombres magiques → constantes

## Objectif

On te donne un code qui **fonctionne déjà** mais qui contient des **nombres magiques** : des
valeurs littérales (`100`, `10`, `20`) dont la signification n'est pas évidente. Refactore-le pour
leur donner des **noms** explicites, **sans changer le comportement**.

Le programme lit un prix entier et calcule le prix TTC : une remise de 10 % si le prix dépasse
100, puis une TVA de 20 % sur le montant net.

Exemple : `200` → `216` (remise 20 → net 180 → +20 % = 216).

## Livrable

- `Prix.cs`

## Contraintes

- Le comportement (donc la sortie) doit rester **strictement identique** : les tests `io` sont ton
  filet de régression.
- Introduis des `const` nommées pour le seuil, le taux de remise et le taux de TVA.

## Indices

- `const int SeuilRemise = 100;` etc., déclarées en haut.
- Refactorer = améliorer la structure **sans** toucher au résultat. Lance `piscine check` après
  chaque petit pas pour vérifier que tout reste vert.
