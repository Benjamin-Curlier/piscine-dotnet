# ex01 — `.dockerignore`

Crée un `.dockerignore` qui exclut au minimum :

- tous les dossiers `bin` et `obj`, quelle que soit leur profondeur ;
- `.git` ;
- les fichiers `.env` ;
- les certificats `*.pfx` ;
- les dossiers ou fichiers nommés `secrets`.

Le but n'est pas seulement d'accélérer le build : tout fichier envoyé dans le contexte peut être lu
par le moteur ou capturé dans une couche si le Dockerfile le copie.
