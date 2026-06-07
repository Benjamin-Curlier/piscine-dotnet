# Mise en œuvre — préparer le poste et démarrer la piscine

Ce guide s'adresse à la **recrue** (pour installer et lancer la piscine) et à l'**encadrant**
(check-list de préparation). Il ne nécessite **aucune installation de SDK .NET** : le binaire est
auto-contenu (runtime .NET + Roslyn embarqués).

> **Ce que contient le zip** : un parcours complet **C# / .NET 10** — modules **M00 à M39**
> (fondamentaux → palier avancé → approfondissement → plateformes & architecture) et **4 Rushes**
> de synthèse. Correction locale par la moulinette (graders `io` / `unit` / `norme` / `mutation` /
> `git` / `projet` / `reseau`), rendu par **vrai git**. **UX recrue** : une **app de bureau** (cours,
> vérification, progression, résultat) **ou** le **CLI** `piscine` — même moteur. Carte détaillée :
> [Curriculum](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/wiki/Curriculum.md).
> Mainteneur qui prépare une release : voir [docs/deploiement.md](deploiement.md).

---

## 1. Prérequis réels

- **Aucun SDK .NET** à installer : le binaire `piscine` est self-contained.
- **Git** :
  - **Windows** : fourni dans le zip (MinGit portable, dossier `mingit/`). Rien à installer.
  - **Linux / macOS** : `git` est presque toujours déjà présent. Sinon :
    - Debian/Ubuntu : `sudo apt install git`
    - Fedora : `sudo dnf install git`
    - macOS : `xcode-select --install` (ou Homebrew : `brew install git`)
- **Webview (pour l'app de bureau)** : l'app `Piscine.Desktop` (en plus du CLI) rend son interface
  dans le **webview système**. Le CLI seul n'en a pas besoin.
  - **Windows** : **WebView2** — préinstallé sur Windows 11 / Windows 10 récents. Éditions N ou images
    minimales : installer l'[Evergreen WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).
  - **Linux** : `sudo apt install libwebkit2gtk-4.1-0` (Debian/Ubuntu) ou
    `sudo dnf install webkit2gtk4.1` (Fedora).
  - **macOS** : rien à installer (WKWebView intégré).
  Détails côté packaging : [docs/deploiement.md](deploiement.md).
- **Espace disque** : ~150 Mo une fois dézippé (runtime + Roslyn inclus).

---

## 2. Installation

1. Télécharger le zip de la **[dernière release](../../releases/latest)** correspondant à l'OS :
   - `piscine-<version>-win-x64.zip` (Windows)
   - `piscine-<version>-linux-x64.zip` (Linux)
   - `piscine-<version>-osx-arm64.zip` (macOS Apple Silicon)
2. **Dézipper** dans un dossier de travail (ex. `C:\piscine` ou `~/piscine`).
3. Selon l'OS :
   - **Windows** : au premier lancement, SmartScreen peut afficher un avertissement
     (« Windows a protégé votre ordinateur ») → *Informations complémentaires* → *Exécuter quand même*.
     Si un fichier est « bloqué » : clic droit → *Propriétés* → cocher *Débloquer*.
   - **Linux / macOS** : rendre le binaire exécutable si besoin :
     ```bash
     chmod +x piscine
     ```
     macOS peut demander d'autoriser le binaire dans *Réglages → Confidentialité et sécurité*.

Le dossier dézippé contient le binaire `piscine` (`piscine.exe` sous Windows), le dossier
`content/` (cours + exercices), l'**app de bureau** dans `desktop/` avec son lanceur
`start-piscine-desktop` (`.cmd` sous Windows, `.sh` sous Linux/macOS) et, sous Windows,
`mingit/` + `start-piscine.cmd`.

---

## 3. Premier lancement

Deux façons d'utiliser la piscine, **au choix**, qui partagent le **même workspace** : l'**app de
bureau** (recommandée — cours, vérification, progression et résultat dans une fenêtre) ou le **CLI**
`piscine` (même moteur, sans fenêtre). Le **rendu** (`git push`) se fait toujours au **terminal** (§4).

### Avec l'app de bureau (recommandé)

1. Lancer l'app (après avoir vérifié les **prérequis webview**, §1) :
   - **Windows** : double-cliquer `start-piscine-desktop.cmd`.
   - **Linux / macOS** : `./start-piscine-desktop.sh`.
2. Dans la fenêtre, ouvrir **Initialiser** et cliquer le bouton : cela met en place le **workspace**,
   le **dépôt bare local** (`origin`) et le **hook** qui lance la moulinette à chaque `git push`.
   *(Équivalent en ligne de commande : `piscine init`.)*
3. Parcourir les **cours** et les **sujets** via le sommaire de gauche ; la suite de la boucle (Vérifier,
   Progression, Résultat) est décrite au §4.

> L'app **n'embarque pas de terminal ni d'éditeur** : on code dans son IDE habituel et on rend
> (`git add/commit/push`) depuis un terminal système (§4).

### Avec le CLI

Ouvrir un terminal dans le dossier dézippé — **Windows** : double-cliquer **`start-piscine.cmd`**, qui
place `piscine` et le git portable (MinGit) sur le PATH — puis :

```bash
piscine init          # workspace + dépôt bare (origin) + hook de correction au push
piscine status        # bannière + état
piscine list          # modules et exercices disponibles
git --version         # (Windows : via start-piscine.cmd ; sinon git système)
```

---

## 4. Boucle de travail

On code dans l'**éditeur/IDE de son choix**. La vérification locale et le suivi se font **dans l'app**
*ou* **au CLI** ; le **rendu officiel** est un `git push`.

### Dans l'app de bureau

1. Choisir un exercice (sommaire → module → exercice) : le sujet et le cours s'affichent.
2. Coder dans son IDE (récupérer le squelette via `piscine start <exo>` au terminal).
3. **Vérifier** : page *Vérifier* → choisir l'exercice → feedback éducatif instantané
   (**ne compte pas**, autant de fois qu'on veut).
4. Suivre l'avancement : page *Progression*.
5. **Rendre** au terminal (ci-dessous), puis regarder la page *Résultat* qui **s'actualise**
   automatiquement après la correction du push.

### Le rendu : `git push` au terminal

Le rendu officiel passe par git, depuis un terminal où `git` **et** `piscine` sont disponibles
(**Windows** : `start-piscine.cmd` ; **Linux / macOS** : un terminal dans le dossier dézippé) :

```bash
piscine start <exo>        # (si besoin) copie le squelette de l'exercice dans le workspace
piscine check <exo>        # équivalent CLI de la page Vérifier (ne compte pas)
git add .
git commit -m "<exo>"
git push origin main       # RENDU OFFICIEL : le hook lance la moulinette et enregistre la progression
```

- `piscine check` / la page *Vérifier* = itération rapide locale, **ne comptent pas** comme rendu.
- `git push origin main` = **rendu officiel** : la moulinette corrige le commit reçu, **par groupe et
  dans l'ordre, en s'arrêtant au premier exercice raté** (les suivants passent en *Non corrigé*),
  affiche le feedback et met à jour la progression (visible dans l'app, page *Résultat*).

---

## 5. Dépannage

- **Antivirus / SmartScreen** : voir §2 (débloquer le binaire).
- **`piscine` introuvable** : se placer dans le dossier dézippé, ou utiliser `start-piscine.cmd`
  (Windows) qui ajoute le dossier au PATH.
- **`git` introuvable (Windows)** : lancer via `start-piscine.cmd` (MinGit n'est sur le PATH
  qu'avec ce lanceur).
- **Variables d'environnement** (optionnelles) :
  - `PISCINE_HOME` : racine du workspace + de l'état (défaut : `~/piscine`).
  - `PISCINE_CONTENT` : dossier de contenu (défaut : `content/` à côté du binaire).
- **Repartir de zéro** : supprimer le dossier d'état (`PISCINE_HOME`, par défaut `~/piscine`)
  puis relancer `piscine init`. Le contenu dézippé n'est pas affecté.

---

## 6. Côté encadrant — check-list

- [ ] Récupérer le zip de la dernière release pour l'OS de la recrue.
- [ ] Vérifier sur un poste vierge : dézipper → `piscine init` → `piscine start <exo>` →
      `piscine check` → `git push` (rendu officiel) fonctionne de bout en bout.
- [ ] S'assurer que la recrue sait lancer un terminal (ou `start-piscine.cmd` sous Windows) pour le
      `git push` du rendu.
- [ ] (App de bureau) prérequis **webview** en place par OS (§1) ; `start-piscine-desktop` ouvre une
      fenêtre qui **route le flux** : cours (titre + bloc de code colorisé), *Vérifier*, *Progression*,
      *Initialiser*, *Résultat*.
- [ ] Remettre le zip (clé USB / partage interne) et ce guide.
- [ ] Rappeler la philosophie : **retour éducatif, pas de note** ; progression auto-rythmée.
