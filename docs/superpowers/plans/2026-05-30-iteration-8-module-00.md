# Itération 8 — Module 00 « Setup & Git » (1er vrai contenu) — Implementation Plan

> **For agentic workers:** itération de **contenu**. Pas de nouveau code C# ; la « vérification » = `piscine validate-content` vert (en local + gate CI). Cases `- [ ]` pour le suivi.

**Goal:** Livrer le premier module réel, `00-setup-git`, qui fait du parcours recrue un dogfood complet : un `cours.md`, et un groupe d'exercices `io` (hello world + salutation par stdin) avec `starter/` et `solution/`. La CI (`validate-content`) devient enfin un **vrai filet** : elle vérifie que les corrigés passent leurs propres graders.

**Architecture / approche :** Contenu data-driven sous `content/modules/00-setup-git/` (découvert au démarrage, sans recompiler). Exercices `io` uniquement (adaptés au tout premier module). Le grader `io` **normalise `\r\n`→`\n`** → `Console.WriteLine` portable. `validate-content` note chaque `solution/<livrable>` via `SubmissionLoader` et exige *Réussi*.

**Tech Stack:** Markdown + YAML + C# (corrigés). Aucune modif de code applicatif.

**Contexte repo (It.0→It.7) :** moteur complet ; `io` (`expect_stdout`/`expect_exit`, stdin/args, timeout 5s, exécution top-level statements via EntryPoint), `unit`, `norme`. `validate-content` (gate CI, `PISCINE_CONTENT=$GITHUB_WORKSPACE/content`) vérifie manifest + `TestFiles` + `solution/` présent + livrables présents + **corrigé passe ses graders**. Format : `module.yaml` (`id/title/order/course/groups[].exercises` ordonnés) ; `manifest.yaml` (`id/title/objective/deliverables/starter/grading[]/feedback`, clés inconnues ignorées). Structure attendue (content/README) : `exercises/<id>/{manifest.yaml,subject.md,starter/,grader/,solution/}`. `package-content` exclut `solution/`. Commandes depuis `C:/Users/bencu/source/repos/piscine-dotnet`. Commits français finis par `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. **`git commit` et `git push` en appels SÉPARÉS.**

---

## File Structure

```
content/modules/00-setup-git/
├── module.yaml
├── cours.md
└── exercises/
    ├── ex00-hello/
    │   ├── manifest.yaml
    │   ├── subject.md
    │   ├── starter/Hello.cs
    │   └── solution/Hello.cs
    └── ex01-salutation/
        ├── manifest.yaml
        ├── subject.md
        ├── starter/Salutation.cs
        └── solution/Salutation.cs
```

> Pas de `grader/` (exercices `io` : les cas sont dans le manifest). `validate-content` n'exige `grader/` que pour `unit`.

---

## Task 1 : Module + cours + `ex00-hello`

- [ ] **Step 1 : `module.yaml`**
```yaml
id: 00-setup-git
title: "Mise en place & premiers pas Git"
order: 0
course: cours.md
groups:
  - id: premiers-pas
    title: "Premiers pas"
    exercises: [ex00-hello, ex01-salutation]
```

- [ ] **Step 2 : `cours.md`** — installer/lancer `piscine`, la boucle de rendu git (`check` vs `git push`), hello world C# (`Console.WriteLine`, `Console.ReadLine`, interpolation `$"..."`), + références externes (Microsoft Learn, freeCodeCamp). Ton pédagogique FR.

- [ ] **Step 3 : `ex00-hello/manifest.yaml`**
```yaml
id: ex00-hello
title: "Hello, Piscine"
objective: "Afficher un message précis sur la sortie standard."
deliverables: [Hello.cs]
starter: [Hello.cs]
grading:
  - type: io
    cases:
      - expect_stdout: "Hello, Piscine!\n"
        expect_exit: 0
feedback:
  hints:
    - when: io_mismatch
      message: "Vérifie la casse, la virgule, le '!' et le retour à la ligne final."
  course_ref: "cours.md#hello-world"
solution: [Hello.cs]
```

- [ ] **Step 4 : `ex00-hello/subject.md`** — énoncé : écrire `Hello.cs` qui affiche exactement `Hello, Piscine!`.

- [ ] **Step 5 : `ex00-hello/starter/Hello.cs`** (squelette à compléter)
```csharp
// Affiche exactement : Hello, Piscine!
// Astuce : System.Console.WriteLine("...");
```

- [ ] **Step 6 : `ex00-hello/solution/Hello.cs`** (corrigé)
```csharp
System.Console.WriteLine("Hello, Piscine!");
```

---

## Task 2 : `ex01-salutation`

- [ ] **Step 1 : `ex01-salutation/manifest.yaml`**
```yaml
id: ex01-salutation
title: "Salutation"
objective: "Lire un prénom sur l'entrée standard et saluer la personne."
deliverables: [Salutation.cs]
starter: [Salutation.cs]
grading:
  - type: io
    cases:
      - stdin: "Alice\n"
        expect_stdout: "Bonjour, Alice!\n"
        expect_exit: 0
      - stdin: "Bob\n"
        expect_stdout: "Bonjour, Bob!\n"
        expect_exit: 0
feedback:
  hints:
    - when: io_mismatch
      message: "Lis le prénom avec Console.ReadLine(), puis affiche 'Bonjour, <prénom>!'."
  course_ref: "cours.md#lire-l-entree"
solution: [Salutation.cs]
```

- [ ] **Step 2 : `subject.md`** — énoncé : lire un prénom, afficher `Bonjour, <prénom>!`.

- [ ] **Step 3 : `starter/Salutation.cs`**
```csharp
// Lis un prénom sur l'entrée standard, puis affiche : Bonjour, <prénom>!
// Astuce : var nom = System.Console.ReadLine();
```

- [ ] **Step 4 : `solution/Salutation.cs`**
```csharp
var nom = System.Console.ReadLine();
System.Console.WriteLine($"Bonjour, {nom}!");
```

---

## Task 3 : Vérification + push + CI

- [ ] **Step 1 : Valider le contenu en local** (PowerShell)
```powershell
$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -- validate-content
```
Expected : « Contenu valide. » (code 0). Si KO : lire les `[exo] message`, corriger le corrigé/manifest, recommencer.

- [ ] **Step 2 : Parcours recrue de bout en bout (sanity)**
```powershell
$env:PISCINE_CONTENT="$PWD/content"; dotnet run --project src/Piscine.Cli -- list
dotnet run --project src/Piscine.Cli -- check ex00-hello   # sans workspace : doit échouer proprement (pas de livrable)
```
Expected : `list` affiche le module 00 et ses 2 exercices ; `check` rend un feedback éducatif (pas un crash).

- [ ] **Step 3 : Suite de tests + format** (rien ne doit casser)
```bash
dotnet test Piscine.slnx --configuration Release
```
Expected : 64 verts (le code n'a pas changé).

- [ ] **Step 4 : Commit + push (appels séparés) + CI**
```bash
git add content/modules/00-setup-git
git commit -m "content(m00): module Setup & Git (hello + salutation, io)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```
puis (séparé) `git push origin main` ; `gh run watch --exit-status`.
Expected : CI verte — **la gate `validate-content` corrige réellement les corrigés du Module 00**.

---

## Self-Review (à compléter à l'exécution)

**Couverture (It.8) :** module `00-setup-git` + cours + 2 exercices `io` (hello, salutation stdin) avec `starter/` et `solution/` validés par `validate-content` (T1–T2), vérif locale + CI (T3). ✓

**Risque & garde-fou :** newline → géré par `Normalize` du grader `io` (`Console.WriteLine` OK). YAML `expect_stdout: "...\n"` (double-quoté) → `\n` interprété ; **prouvé** par le run `validate-content` local (Step 1). Si YamlDotNet n'interprétait pas `\n`, le corrigé échouerait → repli bloc scalaire YAML.

**Reporté :** commande `piscine new exercise <module> <id>` (scaffolding, citée dans la doc, non implémentée) ; It.9+ (M01→M23 + Rushes). Un grader `unit` réel apparaîtra dès qu'un module s'y prête (ex. M13 Tests).
