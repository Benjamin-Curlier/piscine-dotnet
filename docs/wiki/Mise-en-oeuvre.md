# Mise en œuvre

La doc pas-à-pas pour **préparer le poste de la recrue** et démarrer la piscine est versionnée dans
le dépôt :

➡️ **[docs/mise-en-oeuvre.md](https://github.com/Benjamin-Curlier/piscine-dotnet/blob/main/docs/mise-en-oeuvre.md)**

Elle couvre :

- **Prérequis réels** : aucun SDK .NET (binaire self-contained) ; git fourni (MinGit) sous Windows,
  système sous Linux/macOS.
- **Installation** : télécharger le zip de la [dernière release](https://github.com/Benjamin-Curlier/piscine-dotnet/releases/latest)
  selon l'OS, dézipper, débloquer le binaire (Windows) / `chmod +x` (Linux/macOS).
- **Premier lancement** : `piscine init` (workspace + dépôt bare + hook) ; Windows : `start-piscine.cmd`.
- **Boucle de travail** : `piscine start <exo>` → `piscine check` → `git add/commit/push origin main`.
- **Dépannage** : antivirus/SmartScreen, PATH, `PISCINE_HOME` / `PISCINE_CONTENT`, réinitialisation.
- **Côté encadrant** : check-list de préparation poste + remise du zip.

Voir aussi [Workflow de rendu](Workflow-de-rendu) pour le détail des deux boucles (`check` vs `git push`).
