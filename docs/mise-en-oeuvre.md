# Mise en œuvre — préparer le poste et démarrer la piscine

Ce guide s'adresse à la **recrue** (pour installer et lancer la piscine) et à l'**encadrant**
(check-list de préparation). Il ne nécessite **aucune installation de SDK .NET** : le binaire est
auto-contenu (runtime .NET + Roslyn embarqués).

> **Ce que contient le paquet** : un parcours complet **C# / .NET 10** — modules **M00 à M39**
> (fondamentaux → palier avancé → approfondissement → plateformes & architecture) et **4 Rushes**
> de synthèse. Correction locale par la moulinette (graders `io` / `unit` / `norme` / `mutation` /
> `git` / `projet` / `reseau`), rendu par **vrai git**. **UX recrue** : une **app de bureau**
> (tableau de bord, cours, vérification, progression, rapport, réglages, **terminal embarqué +
> coaching git**) **ou** le **CLI** `piscine` — même moteur. Carte détaillée :
> [Curriculum](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/wiki/Curriculum.md).
> Mainteneur qui prépare une release : voir [docs/deploiement.md](deploiement.md).

> **Plateformes** : **Windows** et **Linux**. **macOS n'est plus distribué.**

---

## 1. Prérequis réels

- **Aucun SDK .NET** à installer : le binaire `piscine` et l'app de bureau sont self-contained.
- **Git** :
  - **Windows** : fourni dans le paquet (MinGit portable). Rien à installer.
  - **Linux** : `git` est presque toujours déjà présent. Sinon : Debian/Ubuntu `sudo apt install git`,
    Fedora `sudo dnf install git`.
- **Webview (pour l'app de bureau)** : l'app rend son interface dans le **webview système**. Le CLI
  seul n'en a pas besoin.
  - **Si vous passez par l'installeur** (recommandé, §2) : **rien à faire** côté webview.
    - L'installeur **offline** embarque le runtime et l'installe au besoin (poste hors-ligne géré).
    - L'installeur **online** télécharge le runtime manquant pendant l'installation.
  - **Si vous passez par le zip** (mode portable, §2) : installer le runtime à la main si absent —
    - **Windows** : **WebView2** — préinstallé sur Windows 11 / Windows 10 récents. Éditions N ou images
      minimales : installer l'[Evergreen WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).
    - **Linux** : **`libwebkit2gtk-4.1`** — Debian/Ubuntu `sudo apt install libwebkit2gtk-4.1-0`,
      Fedora `sudo dnf install webkit2gtk4.1`. *(PhotinoX 4.2.0 cible la série 4.1.)*
  Détails côté packaging : [docs/deploiement.md](deploiement.md).
- **Espace disque** : ~150 Mo une fois installé/dézippé (runtime + Roslyn inclus).

---

## 2. Installation

Deux formats au choix : l'**installeur** (recommandé — met l'app dans le menu, gère le webview) ou le
**zip portable** (rien n'est installé sur le poste). Les deux contiennent le **même** moteur, le CLI
`piscine`, l'app de bureau et le contenu.

Chaque format existe en **deux variantes** :

- **offline** — tout est embarqué (runtime webview compris) : fonctionne sur un poste **sans internet**.
- **online** — paquet plus léger : télécharge le runtime webview manquant à l'installation.

### Avec l'installeur (recommandé)

Télécharger depuis la **[dernière release](../../releases/latest)** le fichier correspondant à l'OS :

- **Windows** : `piscine-<version>-win-x64-offline-setup.exe` (ou `-online-setup.exe`).
  - Double-cliquer. L'installeur est **par utilisateur** (pas de droits administrateur).
  - SmartScreen peut s'afficher (binaire non signé) → *Informations complémentaires* → *Exécuter quand même*.
  - Crée des raccourcis **menu Démarrer** + **Bureau** ; installe le runtime WebView2 **s'il manque**.
- **Linux** : `piscine-<version>-linux-x86_64-online.AppImage`.
  ```bash
  chmod +x piscine-<version>-linux-x86_64-online.AppImage
  ./piscine-<version>-linux-x86_64-online.AppImage
  ```
  - L'**AppImage** (online) s'appuie sur le `libwebkit2gtk-4.1` du système (§1) :
    `sudo apt install libwebkit2gtk-4.1-0` si besoin.
  - *(L'AppImage **offline**, webkit embarqué, est abandonnée depuis v3.1.0 — WebKitGTK release ne
    permet pas d'embarquer un webkit fonctionnel. Cf. CHANGELOG v3.1.0.)*

### Avec le zip portable

Télécharger `piscine-<version>-win-x64.zip` (Windows) ou `piscine-<version>-linux-x64.zip` (Linux),
puis **dézipper** dans un dossier de travail (ex. `C:\piscine` ou `~/piscine`).

- **Windows** : au premier lancement, SmartScreen peut afficher un avertissement → *Informations
  complémentaires* → *Exécuter quand même*. Fichier « bloqué » : clic droit → *Propriétés* → *Débloquer*.
- **Linux** : rendre le binaire exécutable si besoin (`chmod +x piscine`).

Le dossier dézippé contient le binaire `piscine` (`piscine.exe` sous Windows), le dossier `content/`
(cours + exercices), l'**app de bureau** dans `desktop/` (avec le shim git `desktop/gitshim/` et son
lanceur `start-piscine-desktop`) et, sous Windows, `mingit/` + `start-piscine.cmd`.

---

## 3. Premier lancement

Deux façons d'utiliser la piscine, **au choix**, qui partagent le **même workspace** : l'**app de
bureau** (recommandée — cours, vérification, progression, résultat **et terminal embarqué**) ou le
**CLI** `piscine` (même moteur, sans fenêtre).

### Avec l'app de bureau (recommandé)

1. Lancer l'app : raccourci **menu Démarrer / Bureau** (installeur), ou `start-piscine-desktop`
   (`.cmd` Windows / `.sh` Linux) dans le dossier du zip. *(Mode zip : vérifier les prérequis webview, §1.)*
2. Si c'est le **premier lancement**, l'app affiche un **guide d'onboarding** : suivre les étapes pour
   mettre en place le **workspace**, le **dépôt bare local** (`origin`) et le **hook** de moulinette.
   *(Équivalent en ligne de commande : `piscine init`. Accessible aussi via l'onglet *Initialiser*.)*
3. Le **tableau de bord** (`/`) oriente d'un coup d'œil : **carte « Reprendre »** vers l'exercice en
   cours, **progression globale** (pourcentages + compteurs par module) et **résultats récents** des
   derniers push.
4. Parcourir les **cours** et les **sujets** via le sommaire de gauche ou l'onglet *Cours* ; la suite
   de la boucle (Vérifier, Progression, Terminal, Résultat) est décrite au §4.

> L'app **embarque un terminal + un coaching git** (page *Terminal*) mais **pas d'éditeur** : on code
> dans son IDE habituel, et on peut faire ses commandes git (`git add/commit/push`) **dans le terminal
> de l'app** — qui affiche un **coaching éducatif** — ou dans un terminal système (§4).
>
> **Astuce** : `Ctrl+K` (ou `⌘K` sur macOS) ouvre la **palette de commande** — recherche plein-texte
> parmi les modules, exercices, pages et actions (Vérifier, Initialiser, Ouvrir…).

### Avec le CLI

Ouvrir un terminal dans le dossier du zip — **Windows** : double-cliquer **`start-piscine.cmd`**, qui
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

1. Depuis le **tableau de bord**, cliquer **« Reprendre »** pour accéder directement au plan de
   travail de l'exercice en cours — ou naviguer manuellement : sommaire → module → exercice.
2. Sur la **page d'exercice**, une barre d'action remplace la consigne CLI :
   - **Ouvrir** : ouvre le dossier de l'exercice dans l'**éditeur détecté** (VS Code, Rider…),
     dans le **Dossier** du système, dans le **terminal intégré** ou dans un **terminal système**.
     Le starter est copié automatiquement si l'exercice n'a pas encore été démarré.
   - **Vérifier** : lance le check in-process → feedback éducatif instantané, avec le **diff
     structuré coloré** attendu/obtenu, l'**indice** et le **lien vers le cours**
     (**ne compte pas**, autant de fois qu'on veut).
   - **Pastille de statut** : indique l'état courant (Réussi / À revoir / Non corrigé).
3. Suivre l'avancement : onglet *Progression* (pastilles de statut dans la navigation) ou
   page *Rapport* (`/rapport`) — vue complète par module, exportable en PDF (impression) ou Markdown.
4. **Rendre** (ci-dessous), puis regarder la page *Résultat* : elle **s'actualise** automatiquement
   après la correction du push et affiche le **résultat riche** — verdict, diff attendu/obtenu, indice
   et lien cours, par exercice. Un **toast** s'affiche aussi partout dans l'app dès que le résultat arrive.

### Le rendu : `git push`

Le rendu officiel passe par git. Deux options équivalentes :

- **Dans le terminal de l'app** (page *Terminal*) : un vrai shell, avec un **coaching git** qui réagit
  à vos commandes (ex. `git commit` sans rien stagé → carte de conseil). Pratique pour rester dans la fenêtre.
- **Dans un terminal système** où `git` **et** `piscine` sont disponibles (**Windows** : `start-piscine.cmd` ;
  **Linux** : un terminal dans le dossier dézippé). *(Sous AppImage Linux, préférer le terminal système
  pour le `git push` du rendu.)*

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
  affiche le feedback et met à jour la progression (visible dans l'app, page *Résultat*, en **riche**).

---

## 5. Dépannage

- **Antivirus / SmartScreen** : binaire non signé — c'est attendu (voir §2 : débloquer / *Exécuter quand même*).
- **`piscine` introuvable** : se placer dans le dossier dézippé, ou utiliser `start-piscine.cmd`
  (Windows) qui ajoute le dossier au PATH.
- **`git` introuvable (Windows)** : lancer via `start-piscine.cmd` (MinGit n'est sur le PATH
  qu'avec ce lanceur).
- **La fenêtre de l'app ne s'ouvre pas (mode zip)** : prérequis **webview** manquant (§1) — préférer
  l'**installeur** (qui le gère), ou installer le runtime à la main (WebView2 / `libwebkit2gtk-4.1`).
- **Variables d'environnement** (optionnelles) :
  - `PISCINE_HOME` : racine du workspace + de l'état (défaut : `~/piscine`).
  - `PISCINE_CONTENT` : dossier de contenu (défaut : `content/` à côté du binaire).
- **Repartir de zéro** : supprimer le dossier d'état (`PISCINE_HOME`, par défaut `~/piscine`)
  puis relancer `piscine init`. Le contenu installé/dézippé n'est pas affecté.

---

## 6. Côté encadrant — check-list

- [ ] Récupérer le paquet de la dernière release pour l'OS de la recrue (**installeur** offline de
      préférence pour un poste hors-ligne ; zip pour un usage portable).
- [ ] Vérifier sur un poste vierge : installer/dézipper → `piscine init` (ou page *Initialiser*) →
      `piscine start <exo>` → `piscine check` → `git push` (rendu officiel) fonctionne de bout en bout.
- [ ] S'assurer que la recrue sait faire son `git push` : **terminal de l'app** (avec coaching) ou
      terminal système (`start-piscine.cmd` sous Windows).
- [ ] (App de bureau) `start-piscine-desktop` (ou le raccourci de l'installeur) ouvre une fenêtre qui
      **route le flux** : **tableau de bord** (reprendre l'exercice courant / progression / résultats
      récents) → cours (titre + bloc de code colorisé) → **plan de travail** (bouton *Ouvrir* +
      *Vérifier* inline + diff coloré) → *Progression* → *Rapport* (exportable PDF/Markdown) →
      *Réglages* (thème, échelle police, éditeur) → *Initialiser* → *Terminal* (git + coaching) →
      *Résultat* (riche). Webview géré par l'installeur, ou présent (§1) en mode zip.
- [ ] (Facultatif) Configurer l'**éditeur** via la page *Réglages* (icône ⚙ dans la navigation) si
      l'auto-détection (VS Code / Rider / Visual Studio) ne correspond pas au poste.
- [ ] Remettre le paquet (clé USB / partage interne) et ce guide.
- [ ] Rappeler la philosophie : **retour éducatif, pas de note** ; progression auto-rythmée.
