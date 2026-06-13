# ex03-decorator — Le pattern Decorator (bonus)

> **Bonus difficile, non bloquant.** Synthèse design patterns : composer des comportements en
> enveloppant un objet (Decorator).

## Énoncé

- **Ligne 1** : un texte.
- **Ligne 2** : une liste de décorations séparées par des virgules, parmi :
  - `maj` → met en **MAJUSCULES** (`ToUpperInvariant`) ;
  - `crochets` → entoure de `[` `]` ;
  - `etoiles` → entoure de `*` `*`.

Applique les décorations **dans l'ordre** (chacune **enveloppe** la précédente — pattern Decorator),
puis affiche le rendu final. **L'ordre change le résultat.**

## Exemple

```
Entrée :
salut
crochets,maj

Sortie :
[SALUT]
```

(`TexteBrut("salut")` → `crochets` → `"[salut]"` → `maj` → `"[SALUT]"`)

## Indications

- Définis `interface ITexte { string Rendu(); }`, une classe `TexteBrut` qui renvoie le texte, et
  **un décorateur par option** : chacun prend un `ITexte` (l'élément enveloppé) et redéfinit `Rendu()`.
- Compose en boucle : pars de `new TexteBrut(texte)`, puis pour chaque décoration remplace l'objet
  courant par `new Decorateur(objetCourant)`.
- `ToUpperInvariant()` pour un résultat déterministe.
