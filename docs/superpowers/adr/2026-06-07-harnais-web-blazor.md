# ADR — Harnais web Blazor (rendu/DOM headless) pour auto-noter des composants

> Issue #19 (suite de #7 : module Blazor M39 livré en **lecture guidée**). Statut : **décision en
> attente du propriétaire** (cet ADR cadre le choix « construire » vs « différer »).

## Contexte
Le module **M39 (Blazor)** est aujourd'hui une **lecture guidée** (`groups: [{ exercises: [] }]`) :
le rendu DOM/web sort du modèle « **console déterministe** » de la moulinette. Pour **auto-noter** un
composant Blazor, il faudrait un harnais capable de **compiler du `.razor`**, **rendre** le composant et
**asserter le markup** de façon déterministe.

### Ce que le moteur sait faire aujourd'hui
- `CompilationService` compile du **C#** via **Roslyn**, en référençant les assemblies **chargées dans le
  process hôte** (`TRUSTED_PLATFORM_ASSEMBLIES`). Le binaire `piscine` / `grade-received` est une **appli
  console** : elle **ne référence pas** ASP.NET Components ni le compilateur Razor.
- Les graders existants (`io`/`unit`/`norme`/`mutation`/`projet`/`reseau`/`git`) sont **déterministes** et
  sans navigateur.

## Ce qu'exigerait #19 (le « gros chantier »)
1. **Compiler `.razor` → C# à l'exécution.** Le `.razor` n'est **pas** compilé par Roslyn brut : il faut le
   **compilateur Razor** (`Microsoft.AspNetCore.Razor.Language`, `RazorProjectEngine`) pour générer le C#,
   puis Roslyn pour l'émettre. La compilation Razor est normalement une étape **SDK/MSBuild** (générateur de
   source) ; la faire **in-process** est tractable mais **fragile** (configuration du `RazorProjectEngine`,
   tag helpers, sensibilité aux versions de SDK/Razor).
2. **Embarquer le renderer Blazor.** Rendre un composant et obtenir du markup ⇒ `Microsoft.AspNetCore.
   Components` + un renderer SSR statique (`HtmlRenderer.RenderComponentAsync<T>()`, .NET 8+). Ces
   assemblies doivent être **chargées dans l'hôte** (pour apparaître dans TPA) ⇒ le binaire self-contained
   **grossit** (ASP.NET Components + Razor en plus du runtime + Roslyn déjà embarqués).
3. **Nouveau grader `blazor`** + **assertions DOM déterministes** (sous-chaîne de markup, présence de
   nœuds/attributs — type bUnit/`HtmlRenderer`). **Limité au SSR statique** : l'interactivité Blazor (JS,
   événements, `@bind` côté client) est **non déterministe** ⇒ **non gradable** par une moulinette console.
4. **Un exercice pilote** Blazor gradable (M39 n'a aujourd'hui **aucun** exo) + son contenu/fixtures.

## Options
| Option | Description | Coût / risque |
|---|---|---|
| **A. Différer (recommandé)** | M39 reste **lecture guidée** ; cet ADR documente le chemin minimal si on revisite | **Nul.** Cohérent avec M22/M34 (modules non auto-notables livrés en lecture) |
| **B. Harnais minimal** | Grader `blazor` = compile `.razor` in-process + `HtmlRenderer` SSR statique + assertions markup ; 1 exo pilote | **Élevé** : nouvelles deps lourdes (Razor.Language + Components) dans le binaire livré, compilation Razor runtime fragile/versionnée, périmètre gradable **étroit** (SSR statique only) |
| **C. Harnais complet (bUnit/interactif)** | Rendu interactif + interactions | **Très élevé / non déterministe** : hors philosophie console ; rejeté |

## Décision (proposée)
**Option A — différer**, sauf demande explicite du propriétaire. Justification :
- **Valeur marginale** : **un seul** module concerné (M39) ; la partie réellement intéressante de Blazor
  (l'**interactivité**) est **non gradable** déterministe ⇒ le sous-ensemble notable (markup SSR statique)
  est étroit et peu représentatif.
- **Coût/risque élevés et durables** : alourdir le **binaire livré** (self-contained) avec ASP.NET Components
  + le compilateur Razor, et maintenir une **compilation Razor runtime** sensible aux versions, pour un gain
  pédagogique faible. Décision **peu réversible** côté dépendances.
- **Cohérence** : M22 (réseau) et M34 (interop) sont aussi livrés en lecture guidée quand la notation
  déterministe n'apporte pas — #19 suit le même principe. **L'issue elle-même** le qualifie de « gros
  chantier moteur, **optionnel**. Le module reste **guidé** en attendant. »

## Chemin minimal viable (si le propriétaire choisit B)
1. **Spike de dérisquage** : compiler un `.razor` trivial (`<h1>@Title</h1>`) **in-process**
   (`RazorProjectEngine` → C# → Roslyn → assembly) et le rendre via `HtmlRenderer` → markup attendu. Prouve
   ou infirme la compilation Razor runtime **avant** tout engagement.
2. Référencer/charger `Microsoft.AspNetCore.Components(.Web)` + `Microsoft.AspNetCore.Razor.Language` dans
   l'hôte ; mesurer la **croissance du binaire** et le **dry-run packaging**.
3. Grader **`blazor`** (type manifest + `BlazorAssertions` : markup `contains`, présence de nœuds/attributs)
   sur le modèle de `projet`/`git` (déterministe, verdict éducatif, jamais de note).
4. **Exo pilote M39** (composant + paramètres, rendu SSR) + bascule lecture → auto-noté ; fixtures gate.
5. Tests + retex + bascule docs.

## Conséquences
- **Si A** : #19 fermée comme **différée** (cet ADR = trace de décision) ; M39 reste guidé ; aucun changement
  moteur ni dépendance ajoutée. Réversible à tout moment (le chemin minimal est documenté ci-dessus).
- **Si B** : nouveau sprint démarrant par le **spike** (étape 1) — go/no-go avant d'embarquer les deps.
