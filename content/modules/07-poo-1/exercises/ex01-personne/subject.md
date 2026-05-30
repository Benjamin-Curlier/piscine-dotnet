# ex01-personne — Personne

## Objectif

Lis un **nom** (une ligne) puis un **âge** (un entier). Crée une `Personne` et affiche sa
présentation, produite par une méthode `SePresenter()`, au format **exact** :

```
Je m'appelle Alice, j'ai 30 ans.
```

Exemples : `Alice` / `30` → `Je m'appelle Alice, j'ai 30 ans.` · `Bob` / `25` → `Je m'appelle Bob, j'ai 25 ans.`

## Livrable

- `Personne.cs`

## Indices

- Classe `Personne` avec `Nom` (`string`) et `Age` (`int`) en propriétés.
- `public string SePresenter() => $"Je m'appelle {Nom}, j'ai {Age} ans.";`.
- Attention à l'apostrophe et au point final.
