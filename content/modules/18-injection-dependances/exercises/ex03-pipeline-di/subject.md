# ex03-pipeline — Pipeline par injection de dépendances (bonus)

> **Bonus difficile, non bloquant.** Synthèse DI : enregistrer, injecter par constructeur, résoudre.

## Énoncé

Construis un conteneur DI (`ServiceCollection` → `BuildServiceProvider`) qui enregistre :

- `IFormateur` → `FormateurMajuscule` : met le texte en **MAJUSCULES** (`ToUpperInvariant`) ;
- `Rapport` : dépend de `IFormateur` (**injection par constructeur**) ; `Produire(t)` renvoie `[<t formaté>]`.

Résous un `Rapport`, puis pour **chaque ligne** lue (jusqu'à la fin de l'entrée), affiche
`rapport.Produire(ligne)`.

## Exemple

```
Entrée :
bonjour
le monde

Sortie :
[BONJOUR]
[LE MONDE]
```

## Indications

- `services.AddSingleton<IFormateur, FormateurMajuscule>();` puis `services.AddTransient<Rapport>();`.
- `provider.GetRequiredService<Rapport>()` construit le `Rapport` en lui injectant le `IFormateur`.
- Utilise `ToUpperInvariant()` (déterministe, indépendant de la culture).
