# ex02-dispose-idempotent — Dispose idempotent

## Objectif

Un bon `Dispose()` doit pouvoir être appelé **plusieurs fois sans danger** : seul le premier appel
libère réellement, les suivants ne font rien. C'est ce qu'on appelle l'**idempotence**, et c'est
indispensable car `Dispose` peut être appelé à la fois manuellement et par un `using`.

Lis un nom. Crée la ressource, puis appelle `Dispose()` **deux fois**. La sortie ne doit afficher
`ferme` qu'**une seule** fois.

Exemple : `db` →
```
ouvre db
ferme db
```

## Livrable

- `Connexion.cs`

## Contraintes

- `Dispose()` est **idempotent** : protégé par un drapeau booléen.

## Indices

- Champ `private bool _ferme;`. Dans `Dispose()` : `if (_ferme) { return; }` puis `_ferme = true;`
  avant d'afficher `ferme`.
- C'est le cœur du « Dispose pattern » : se prémunir contre les appels multiples.
