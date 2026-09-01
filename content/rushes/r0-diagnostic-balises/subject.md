# Rush 0 — Diagnostic de balises

> Un Rush est une mission de synthèse solo. Tu réutilises ici les modules 00 à 04 : saisie console,
> conditions, boucles, tableaux et manipulation de chaînes.

## Situation

Le centre Asteria reçoit l'état de `N` balises, dans leur ordre physique. Une ligne contient un nom
sans espace et l'un des états `OK`, `WARN` ou `KO` :

```text
NOM ETAT
```

Mémorise les noms et les états dans des tableaux. Produis ensuite :

1. la ligne `INCIDENTS` ;
2. chaque balise qui n'est pas `OK`, dans l'ordre d'entrée, sous la forme `NOM: ETAT` ;
3. la ligne `aucun` s'il n'existe aucun incident ;
4. le résumé exact `Résumé: OK=X WARN=Y KO=Z`.

Les entrées respectent toujours le contrat et `N` est au moins égal à 1.

## Exemple

Entrée :

```text
3
alpha OK
beta WARN
gamma KO
```

Sortie :

```text
INCIDENTS
beta: WARN
gamma: KO
Résumé: OK=1 WARN=1 KO=1
```

## Livrable

- `Diagnostic.cs`

Le résultat doit être obtenu à partir des données lues. Aucun nom de balise ni compteur ne doit être
codé en dur.

## Rendu

Travaille dans ton workspace, puis utilise le vrai cycle Git : `add`, `commit` et `push origin main`.
