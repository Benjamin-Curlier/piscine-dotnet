# ex03-anagramme — Anagrammes (bonus)

> Exercice **bonus** — difficulté **difficile**.

## Objectif

Lis **deux mots** (un par ligne). Affiche `oui` si les deux mots sont **anagrammes** l'un de l'autre
(ils contiennent exactement les mêmes lettres, dans un ordre différent), `non` sinon.

La comparaison est **insensible à la casse**. Deux mots **identiques** ne sont **pas** considérés
comme des anagrammes.

Exemple : `chien` et `niche` → `oui` ; `kayak` et `kayak` → `non`.

## Livrable

- `Anagramme.cs`

## Indices

- Passe les deux mots en minuscules avec `.ToLower()`.
- Trie les caractères de chaque mot : `mot.OrderBy(c => c).ToArray()` (nécessite `using System.Linq;`).
- Compare les deux tableaux triés avec `.SequenceEqual(...)`.
- Ajoute la condition que les mots d'origine soient différents (`mot1 != mot2`).
