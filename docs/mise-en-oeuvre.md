# Mise en œuvre — préparer le poste et démarrer la piscine

Ce guide s'adresse à la **recrue** (pour installer et lancer la piscine) et à l'**encadrant**
(check-list de préparation). Il ne nécessite **aucune installation de SDK .NET** : le binaire est
auto-contenu (runtime .NET + Roslyn embarqués).

---

## 1. Prérequis réels

- **Aucun SDK .NET** à installer : le binaire `piscine` est self-contained.
- **Git** :
  - **Windows** : fourni dans le zip (MinGit portable, dossier `mingit/`). Rien à installer.
  - **Linux / macOS** : `git` est presque toujours déjà présent. Sinon :
    - Debian/Ubuntu : `sudo apt install git`
    - Fedora : `sudo dnf install git`
    - macOS : `xcode-select --install` (ou Homebrew : `brew install git`)
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
`content/` (cours + exercices) et, sous Windows, `mingit/` + `start-piscine.cmd`.

---

## 3. Premier lancement

### Windows
Double-cliquer **`start-piscine.cmd`** : il place `piscine` et le git portable sur le PATH et
ouvre une invite prête à l'emploi. Puis :

```bat
piscine init
```

### Linux / macOS
Ouvrir un terminal dans le dossier dézippé :

```bash
./piscine init
```

`piscine init` met en place :
- le **workspace** (espace de code de la recrue) ;
- un **dépôt bare local** qui joue le rôle du « GitLab » (`origin`) ;
- un **hook** qui lance automatiquement la moulinette à chaque `git push`.

Vérifier ensuite que tout répond :

```bash
piscine status        # bannière + état
piscine list          # modules et exercices disponibles
git --version         # (Windows : via start-piscine.cmd ; sinon git système)
```

---

## 4. Boucle de travail

```bash
piscine start <exo>        # copie le squelette de l'exercice dans le workspace
# ... la recrue code dans le workspace ...
piscine check <exo>        # feedback éducatif instantané, autant de fois qu'on veut (ne compte pas)
git add .
git commit -m "<exo>"
git push origin main       # RENDU OFFICIEL : le hook lance la moulinette et enregistre la progression
```

- `piscine check` = itération rapide locale, **ne compte pas** comme rendu.
- `git push origin main` = **rendu officiel** : la moulinette corrige le commit reçu,
  **par groupe et dans l'ordre, en s'arrêtant au premier exercice raté** (les suivants
  passent en *Non corrigé*), affiche le feedback et met à jour la progression.

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
- [ ] S'assurer que la recrue sait lancer un terminal (ou `start-piscine.cmd` sous Windows).
- [ ] Remettre le zip (clé USB / partage interne) et ce guide.
- [ ] Rappeler la philosophie : **retour éducatif, pas de note** ; progression auto-rythmée.
