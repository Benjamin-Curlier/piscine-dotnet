# ex01 — Retry borné

La première ligne donne `maxAttempts`, la seconde une séquence de résultats `TEMP`, `FATAL` ou `OK`.
Consomme au plus le budget :

- `OK` affiche `SUCCESS n` et arrête ;
- `TEMP` permet de retenter s'il reste du budget ;
- `FATAL` affiche `FAILURE n` et arrête immédiatement ;
- budget épuisé sans succès : `FAILURE maxAttempts`.

`n` est le nombre de tentatives réellement consommées.
