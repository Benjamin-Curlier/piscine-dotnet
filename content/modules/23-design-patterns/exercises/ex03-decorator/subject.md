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

(`TexteSimple("salut")` → `Crochets(TexteSimple)` → `Majuscule(Crochets)` → `"[SALUT]"`)

## Indications

- Définis `interface ITexte { string Rendu(); }`, une classe `TexteSimple` qui renvoie le texte, et
  **un décorateur par option** : chacun prend un `ITexte` (l'élément enveloppé) et redéfinit `Rendu()`.
- Garde une variable `ITexte courant = new TexteSimple(texte)`. Pour chaque nom lu, crée le décorateur
  correspondant **autour de `courant`**, puis remets le nouvel objet dans `courant`.
- Exemple concret : après `crochets`, `courant` désigne `new Crochets(ancienCourant)` ; après `maj`,
  il désigne `new Majuscule(new Crochets(new TexteSimple(texte)))`.
- `ToUpperInvariant()` pour un résultat déterministe.
