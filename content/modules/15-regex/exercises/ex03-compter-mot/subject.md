# ex03-compter-mot — Compter un mot entier (bonus)

> **Bonus difficile, non bloquant.** Synthèse regex : limites de mot, insensibilité à la casse,
> échappement.

## Énoncé

- **Ligne 1** : un mot.
- **Ligne 2** : un texte.

Affiche le **nombre d'occurrences** du mot dans le texte, compté comme **mot entier** (limites de
mot `\b`, donc `le` ne compte pas dans `vélo`) et **sans tenir compte de la casse**.

## Exemple

```
Entrée :
le
le chat et LE chien sur le toit

Sortie :
3
```

## Indications

- Utilise `Regex.Matches(texte, motif, RegexOptions.IgnoreCase).Count`.
- Le motif entoure le mot de `\b` (limite de mot) : `\bmot\b`.
- Échappe le mot recherché avec `Regex.Escape(mot)` (au cas où il contiendrait des métacaractères).
- Une chaîne verbatim `@"..."` ou une interpolation `$@"\b{...}\b"` évite de doubler les `\`.
