# ex03-using-exception — Libération garantie malgré une exception

## Objectif

Le grand intérêt de `using` (par rapport à un appel manuel à `Dispose`) : la ressource est libérée
**même si une exception est levée**. Le `Dispose()` s'exécute pendant le déroulement de la pile,
avant que l'exception ne soit attrapée plus haut.

Lis un nom. Dans un `try`, ouvre une ressource en `using`, affiche `debut`, puis lève une
exception. Un `catch` affiche `erreur attrapee`. La sortie doit prouver que `ferme` survient
**avant** `erreur attrapee`.

Exemple : `t1` →
```
ouvre t1
debut
ferme t1
erreur attrapee
```

## Livrable

- `Transaction.cs`

## Contraintes

- La ressource est ouverte en `using` à l'intérieur du `try`.
- Ne mets pas le `Dispose` à la main : c'est le `using` qui doit garantir la libération.

## Indices

- `try { using var t = new Transaction(nom); ... throw new System.Exception("..."); } catch { ... }`.
- À la levée, la portée du `using` (le `try`) se termine → `Dispose()` est appelé, *puis*
  l'exception remonte jusqu'au `catch`.
