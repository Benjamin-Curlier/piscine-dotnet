# ex01-ordre-lifo — Ordre de libération (LIFO)

## Objectif

Quand plusieurs ressources sont ouvertes, elles doivent être libérées dans l'ordre **inverse** de
leur ouverture (comme une pile : *Last In, First Out*). C'est exactement ce que font les
**using declarations** à la fin de leur portée.

Lis trois noms séparés par des espaces. Ouvre trois ressources, affiche `travail`, et laisse-les
se libérer. La sortie doit montrer les ouvertures dans l'ordre, puis les fermetures à l'envers.

Exemple : `A B C` →
```
ouvre A
ouvre B
ouvre C
travail
ferme C
ferme B
ferme A
```

## Livrable

- `Ressources.cs`

## Contraintes

- Utilise des **using declarations** (`using var x = ...;`), pas des blocs imbriqués manuels.
- L'ordre de fermeture doit être strictement l'inverse de l'ouverture.

## Indices

- `using var a = new Ressource(noms[0]);` etc. Les trois sont libérées à la fin du programme.
- Le compilateur génère la libération en ordre inverse de déclaration — tu n'as rien à coder pour
  l'ordre, juste à utiliser les using declarations.
