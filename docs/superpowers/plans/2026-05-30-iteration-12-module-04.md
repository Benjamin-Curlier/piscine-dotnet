# Itération 12 — Module 04 « Tableaux & chaînes » — Implementation Plan

> **For agentic workers:** itération de **contenu**. Vérification = `validate-content` vert (local + gate CI).

**Goal:** Livrer le Module 04 `04-tableaux-chaines` : tableaux (`array`, indice, `Length`, parcours), chaînes (`char`, `Length`, `Split`, `ToCharArray`, `Reverse`, `ToLower`/`Contains`). Trois exercices `io` (somme d'un tableau, inverser une chaîne, compter les voyelles). Note git : `.gitignore`.

**Architecture / approche :** Modèle `io` habituel. Combine tableaux ET chaînes : `Split`→tableau (somme), `ToCharArray`+`Array.Reverse` (inverser), itération `char` (voyelles).

**Tech Stack:** Markdown + YAML + C#.

---

## File Structure

```
content/modules/04-tableaux-chaines/
├── module.yaml          # order: 4, groupe "tableaux-chaines": [ex00-somme-tableau, ex01-inverser, ex02-voyelles]
├── cours.md             # tableaux, chaînes, Split/ToCharArray/Reverse, .gitignore
└── exercises/
    ├── ex00-somme-tableau/ {manifest, subject, starter/SommeTableau.cs, solution/SommeTableau.cs}
    ├── ex01-inverser/      {manifest, subject, starter/Inverser.cs, solution/Inverser.cs}
    └── ex02-voyelles/      {manifest, subject, starter/Voyelles.cs, solution/Voyelles.cs}
```

---

## Tasks

- [x] `module.yaml` (order 4) + `cours.md` (tableaux : indice/`Length`/parcours ; chaînes : `char`/`Split`/`ToCharArray`/`Reverse`/`ToLower`/`Contains` ; encart `.gitignore`).
- [x] `ex00-somme-tableau` : `Split(' ', RemoveEmptyEntries)` → somme. Cas `1 2 3 4→10`, `10 -2 5→13`, `42→42`.
- [x] `ex01-inverser` : `ToCharArray`+`Array.Reverse`+`new string`. Cas `hello→olleh`, `Piscine→enicsiP`, `a→a`.
- [x] `ex02-voyelles` : itération `char`, `"aeiou".Contains(char.ToLower(c))`. Cas `bonjour→3`, `Piscine→3`, `xyz→0`, `AEIOU→5`.
- [ ] **Vérif** : `validate-content` → « Contenu valide. » ; `list` montre M00→M04 ; suite 73 verts.
- [ ] **Commit** `content(m04): module Tableaux & chaînes (somme-tableau, inverser, voyelles, io)` + push séparé + `gh run watch`.

---

## Self-Review

**Couverture (It.12) :** module `04-tableaux-chaines` + cours + 3 exercices `io` (tableau via Split, tableau de char + Reverse, itération string) validés par `validate-content`. ✓
**Risque :** `Split` et espaces multiples → `RemoveEmptyEntries`. `char.ToLower` pour majuscules. Prouvé par `validate-content`.
**Reporté :** **Rush 0** (après M04, projet console — format à concevoir) ; It.13 = M05 Git intermédiaire (module dédié, pas un simple `io`) ; grader `unit` réel à M13.
