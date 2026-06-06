# Piscine .NET — Wiki

Bootcamp d'onboarding façon « piscine » Epitech/42, ciblant les fondamentaux **C# pur** (.NET 10),
avec une **moulinette auto-correctrice locale**, l'apprentissage du **vrai git**, et une
**distribution autonome** (zip self-contained, zéro install).

Ce wiki s'adresse aux **encadrants** et **contributeurs**. La doc de prise en main de la recrue
est dans le dépôt : [docs/mise-en-oeuvre.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/mise-en-oeuvre.md).

## Philosophie (décisions figées)

- **Retour éducatif, jamais de note.** La moulinette explique *attendu vs obtenu*, donne des
  indices et renvoie vers le cours. Statuts : **Réussi / À revoir / Non corrigé**. Aucun score chiffré.
- **Correction par groupe, séquentielle, arrêt au premier échec.** Dans un groupe ordonné, si un
  exercice est *À revoir*, les suivants passent en *Non corrigé* (comme la trace 42).
- **Aucune pression de temps.** Progression modulaire auto-rythmée ; tout reste accessible.
- **Extensibilité data-driven.** Ajouter un exercice = déposer des fichiers, **sans recompiler**.
- **Zéro pré-requis.** Roslyn embarqué (compilation C# sans SDK) ; git portable (MinGit) bundlé sous Windows.
- **Français** partout (cours, sujets, messages).

## Comment ça marche, en une phrase

La recrue dézippe le bootcamp, lance `piscine init` (qui crée un dépôt git « GitLab » local), code
les exercices dans son workspace, itère avec `piscine check`, puis **rend officiellement par
`git push`** — ce qui déclenche la moulinette sur le commit reçu.

## Sommaire

- **[Fonctionnement de la moulinette](Moulinette)** — Roslyn, graders `io`/`unit`/`norme`, correction par groupe.
- **[Workflow de rendu](Workflow-de-rendu)** — dépôt bare local, hook `post-receive`, `check` vs `git push`.
- **[Ajouter un exercice](Ajouter-un-exercice)** — format `manifest.yaml`/`module.yaml`, `validate-content`.
- **[Curriculum](Curriculum)** — carte des modules et Rushes, références externes.
- **[Mise en œuvre](Mise-en-oeuvre)** — préparer le poste de la recrue.

## Liens

- Dépôt : <https://github.com/Benjamin-Curlier/piscine-dotnet>
- Design complet : [docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/superpowers/specs/2026-05-29-piscine-dotnet-design.md)
- Publier une release (mainteneur) : [docs/deploiement.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/deploiement.md)
- Journal des versions : [CHANGELOG.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/CHANGELOG.md)
- Releases (zips par OS) : <https://github.com/Benjamin-Curlier/piscine-dotnet/releases/latest>
