# Log structuré

Un bon log n'est pas une chaîne déjà assemblée : c'est un **modèle** (« message à trous ») plus
des **valeurs** nommées. Les outils peuvent alors indexer/filtrer par valeur (`Id`, `Client`…).

## Objectif

Lis un identifiant (entier) puis un nom de client. Avec un logger de catégorie `Commandes`
(niveau minimum Information), émets **un** log Information avec un message à trous :

```csharp
logger.LogInformation("Commande {Id} validée pour {Client}", id, client);
```

Les trous `{Id}` et `{Client}` sont remplis **dans l'ordre des arguments**.

`LogCapture.cs` t'est fourni (ne le modifie pas).

## Exemple

Entrée :

```
42
Alice
```

Sortie :

```
Commandes [Information] Commande 42 validée pour Alice
```

## Livrables

- `Structure.cs`
- `LogCapture.cs` (fourni, à rendre tel quel)
