# Itération 14 — Module 06 « Collections » — Implementation Plan

> **For agentic workers:** itération de **contenu**. M05 (Git intermédiaire, format à concevoir) SAUTÉ pour l'instant (order 5 laissé libre). Vérif = `validate-content` vert.

**Goal:** Livrer le Module 06 `06-collections` : `List<T>`, `Dictionary<TKey,TValue>`, premier contact LINQ. Trois exercices `io` (trier une liste, fréquence d'un mot, somme des pairs via LINQ).

**Architecture / approche :** Modèle `io`. Progression List → Dictionary → LINQ. ex00/ex01 sans LINQ (List/Dictionary purs, types pleinement qualifiés `System.Collections.Generic.*`) ; ex02 introduit `using System.Linq;` (`Select`/`Where`/`Sum`) — prouve que le grader Roslyn référence System.Linq via TPA et accepte un `using` en tête de fichier top-level.

**Tech Stack:** Markdown + YAML + C#.

---

## Tasks

- [x] `module.yaml` (order 6, groupe `listes-dictionnaires`) + `cours.md` (List, Dictionary, GetValueOrDefault, intro LINQ, note namespaces/using).
- [x] `ex00-tri-liste` : `List<int>` + `Sort` + `string.Join`. Cas `3 1 2→1 2 3`, `5→5`, `10 -3 7 -3→-3 -3 7 10`.
- [x] `ex01-frequence` : `Dictionary<string,int>` (GetValueOrDefault+1) + cible. Cas `a b a c a`/`a`→3, `chat chien chat`/`chien`→1, `x y z`/`w`→0.
- [x] `ex02-somme-pairs` : `using System.Linq;` + `.Select(int.Parse).Where(x=>x%2==0).Sum()`. Cas `1 2 3 4→6`, `1 3 5→0`, `2 4 6→12`, `10 -4 7→6`.
- [ ] **Vérif** : `validate-content` → « Contenu valide. » (valide notamment la compilation LINQ) ; `list` montre M00→M04, M06, Rush 0 ; suite 77 verts.
- [ ] **Commit** `content(m06): module Collections (tri-liste, frequence, somme-pairs, io)` + push séparé + `gh run watch`.

---

## Self-Review

**Couverture (It.14) :** module `06-collections` + cours + 3 exercices `io` (List, Dictionary, LINQ) validés par `validate-content`. ✓
**Risque :** LINQ exige `using System.Linq;` (pas d'implicit usings dans le grader — confirmé par le style « System.Console » des modules précédents) → prouvé par `validate-content`. `string.Join(' ', List<int>)` OK (.NET).
**Reporté :** **M05 Git intermédiaire** (order 5, format dédié à concevoir) ; M07 POO 1 ; grader `unit` réel à M13 ; Rush 1 après ~M08.
