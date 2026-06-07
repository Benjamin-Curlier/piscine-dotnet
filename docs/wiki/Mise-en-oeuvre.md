# Mise en œuvre

La doc pas-à-pas pour **préparer le poste de la recrue** et démarrer la piscine est versionnée dans
le dépôt :

➡️ **[docs/mise-en-oeuvre.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/mise-en-oeuvre.md)**

Elle couvre (**Windows + Linux** ; macOS n'est plus distribué) :

- **Prérequis réels** : aucun SDK .NET (binaire self-contained) ; git fourni (MinGit) sous Windows,
  système sous Linux ; webview géré par l'installeur (ou WebView2 / `libwebkit2gtk-4.0` en mode zip).
- **Installation** : **installeur** (recommandé — Windows `.exe` per-utilisateur / Linux `.AppImage`,
  variantes **offline**/online) **ou** zip portable, depuis la
  [dernière release](https://github.com/Benjamin-Curlier/piscine-dotnet/releases/latest).
- **Premier lancement** : app de bureau (cours, *Vérifier*, *Progression*, *Terminal* + coaching git,
  *Résultat* riche) **ou** CLI `piscine init` ; Windows : `start-piscine.cmd`.
- **Boucle de travail** : `piscine start <exo>` → `piscine check` → `git add/commit/push origin main`
  (au **terminal de l'app** avec coaching, ou au terminal système).
- **Dépannage** : antivirus/SmartScreen, PATH, `PISCINE_HOME` / `PISCINE_CONTENT`, réinitialisation.
- **Côté encadrant** : check-list de préparation poste + remise du paquet.

Voir aussi [Workflow de rendu](Workflow-de-rendu) pour le détail des deux boucles (`check` vs `git push`).
