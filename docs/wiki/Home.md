# Piscine .NET — Wiki

Bootcamp d'onboarding façon « piscine » Epitech/42, ciblant les fondamentaux **C# pur** (.NET 10),
avec une **moulinette auto-correctrice locale**, l'apprentissage du **vrai git**, et une
**distribution autonome** (installeurs ou zips self-contained, zéro SDK). **Windows et Linux.**

La recrue a deux UX au choix (même moteur) : une **app de bureau** (tableau de bord, cours,
**plan de travail + bouton Ouvrir**, vérification avec diff coloré, progression, rapport exportable,
réglages, **terminal + coaching git**, résultat riche) ou le **CLI** `piscine`.

Ce wiki s'adresse aux **encadrants** et **contributeurs**. La doc de prise en main de la recrue
est dans le dépôt : [docs/mise-en-oeuvre.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/mise-en-oeuvre.md).

## Philosophie (décisions figées)

- **Retour éducatif, jamais de note.** La moulinette explique *attendu vs obtenu*, donne des
  indices et renvoie vers le cours. Statuts : **Réussi / À revoir / Non corrigé**. Aucun score chiffré.
- **Correction par groupe, séquentielle, arrêt au premier échec.** Dans un groupe ordonné, si un
  exercice est *À revoir*, les suivants passent en *Non corrigé* (comme la trace 42).
- **Aucune pression de temps.** Progression modulaire auto-rythmée ; tout reste accessible.
- **Extensibilité data-driven.** Ajouter un exercice = déposer des fichiers, **sans recompiler**.
- **Zéro pré-requis.** Roslyn embarqué (compilation C# sans SDK) ; git portable (MinGit) bundlé sous
  Windows ; webview géré par l'installeur (ou WebView2 / `libwebkit2gtk-4.1` en mode zip).
- **Les corrigés ne sont jamais distribués.** Les dossiers `solution/` servent la gate `validate-content`
  (CI) mais sont **exclus** du paquet remis à la recrue (`package-content`).
- **Français** partout (cours, sujets, messages).

## Comment ça marche, en une phrase

La recrue installe le bootcamp ; au premier lancement l'app l'**guide via un onboarding** (ou
`piscine init` en CLI — crée un dépôt git « GitLab » local). Elle consulte le **tableau de bord**
pour reprendre là où elle en est, ouvre l'exercice en un clic (**bouton Ouvrir** → IDE/dossier/terminal),
itère avec **Vérifier** (diff structuré coloré, ne compte pas), puis **rend officiellement par
`git push`** — au terminal de l'app (avec coaching) ou système — ce qui déclenche la moulinette sur
le commit reçu, dont le verdict riche s'affiche dans **Résultat** (+ toast global dans l'app).

## Sommaire

- **[Fonctionnement de la moulinette](Moulinette)** — Roslyn, graders `io`/`unit`/`norme`/`mutation`/`git`/`projet`/`reseau`, correction par groupe.
- **[Workflow de rendu](Workflow-de-rendu)** — dépôt bare local, hook `post-receive`, `check` vs `git push`, app de bureau.
- **[Ajouter un exercice](Ajouter-un-exercice)** — format `manifest.yaml`/`module.yaml`, `validate-content`.
- **[Curriculum](Curriculum)** — carte des modules et Rushes, références externes.
- **[Mise en œuvre](Mise-en-oeuvre)** — préparer le poste de la recrue.

## Liens

- Dépôt : <https://github.com/Benjamin-Curlier/piscine-dotnet>
- Design complet : [docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md)
- Publier une release (mainteneur) : [docs/deploiement.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/deploiement.md)
- Journal des versions : [CHANGELOG.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/CHANGELOG.md)
- Releases (zips par OS) : <https://github.com/Benjamin-Curlier/piscine-dotnet/releases/latest>
