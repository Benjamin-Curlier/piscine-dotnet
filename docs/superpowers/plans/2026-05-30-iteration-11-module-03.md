# Itération 11 — Module 03 « Méthodes » — Implementation Plan

> **For agentic workers:** itération de **contenu**. Premier module créé en **dogfood** de `piscine new exercise` (It.10b). Vérification = `validate-content` vert (local + gate CI).

**Goal:** Livrer le Module 03 `03-methodes` : déclarer/appeler des méthodes, paramètres, type de retour, portée, récursion. Trois exercices `io` (carré, max de trois, factorielle récursive) avec `starter/` + `solution/`, un `cours.md`.

**Architecture / approche :** Modèle `io` des modules précédents. Les solutions utilisent des **fonctions locales** (`static T F(...)` en bas du fichier top-level) pour matérialiser la notion de méthode. Récursion sur la factorielle (retour `long`). Squelettes générés via `piscine new exercise 03-methodes <id>` (dogfood It.10b), puis remplis.

**Tech Stack:** Markdown + YAML + C#.

---

## File Structure

```
content/modules/03-methodes/
├── module.yaml          # order: 3, groupe "parametres-retour": [ex00-carre, ex01-max3, ex02-factorielle]
├── cours.md             # déclaration/appel, paramètres/retour, expression-bodied, portée, récursion
└── exercises/
    ├── ex00-carre/       {manifest, subject, starter/Carre.cs, solution/Carre.cs}
    ├── ex01-max3/        {manifest, subject, starter/Max3.cs, solution/Max3.cs}
    └── ex02-factorielle/ {manifest, subject, starter/Factorielle.cs, solution/Factorielle.cs}
```

---

## Tasks

- [x] `module.yaml` (order 3) + `cours.md` (méthode, paramètres, retour, `=>`, portée, récursion, note top-level/fonctions locales).
- [x] **Dogfood** : `piscine new exercise 03-methodes ex00-carre|ex01-max3|ex02-factorielle` → squelettes (livrables `Carre.cs`/`Max3.cs`/`Factorielle.cs` déduits) ; puis remplissage avec le vrai contenu.
- [x] `ex00-carre` : méthode `Carre(int)=>n*n`. Cas `5→25`, `0→0`, `12→144`.
- [x] `ex01-max3` : méthode `Max(int,int)` réutilisée `Max(a, Max(b,c))`. Cas `3,9,5→9`, `10,2,7→10`, `1,1,1→1`, `-4,-9,-1→-1`.
- [x] `ex02-factorielle` : récursion `n<=1?1:n*Factorielle(n-1)` (retour `long`). Cas `5→120`, `0→1`, `1→1`, `6→720`.
- [ ] **Vérif** : `validate-content` → « Contenu valide. » ; `list` montre M00→M03 ; suite 73 verts.
- [ ] **Commit** `content(m03): module Méthodes (carre, max3, factorielle, io)` + push séparé + `gh run watch`.

---

## Self-Review

**Couverture (It.11) :** module `03-methodes` + cours + 3 exercices `io` (méthode simple, méthode réutilisée, récursion) validés par `validate-content`. Premier dogfood réussi de `new exercise`. ✓
**Risque :** fonctions locales en top-level → doivent suivre les instructions ; prouvé par `validate-content`. Factorielle en `long` (débordement évité sur les cas testés).
**Reporté :** It.12+ (M04 Tableaux & chaînes → M23) + Rushes ; grader `unit` réel à M13.
