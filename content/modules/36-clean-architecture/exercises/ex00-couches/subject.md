# Gestionnaire de tâches en couches

Tu vas écrire une petite application **structurée en couches** (Clean Architecture) : un
gestionnaire de tâches piloté en ligne de commande. L'enjeu n'est pas l'algorithme — il est
**simple** — mais le **respect de la règle de dépendance** entre les couches.

## Les couches attendues

| Couche | Fichier | Rôle | Dépend de |
|---|---|---|---|
| Domain | `Domain/Tache.cs` | entité `Tache` | rien |
| Domain | `Domain/IDepotTaches.cs` | port `IDepotTaches` | rien |
| Application | `Application/GestionTaches.cs` | cas d'usage | Domain |
| Infrastructure | `Infrastructure/DepotMemoire.cs` | adaptateur en mémoire | Domain |
| (racine) | `Program.cs` | composition root + entrée | toutes |

> **Règle de dépendance** : `Domain` ne référence ni `Application` ni `Infrastructure` ;
> `Application` ne dépend **que** du port `IDepotTaches` (pas de `DepotMemoire`). Seul `Program`
> câble les implémentations concrètes. La moulinette **vérifie ces dépendances**.

## Le comportement

Lis un entier `N`, puis `N` lignes de commandes :

- `add <titre>` → crée une tâche (id auto-incrémenté à partir de 1) et affiche
  `Ajoutée : #<id> <titre>` ;
- `done <id>` → marque la tâche faite et affiche `Faite : #<id>` ; si l'id n'existe pas,
  affiche `Inconnue : #<id>` ;
- `list` → affiche une ligne par tâche, dans l'ordre de création :
  `#<id> [x] <titre>` si elle est faite, `#<id> [ ] <titre>` sinon.

À la fin (après les `N` commandes), affiche toujours :
`Résumé : <total> tâche(s), <faites> faite(s)`.

### Exemple

Entrée :

```
4
add Acheter du pain
add Lire un livre
done 1
list
```

Sortie :

```
Ajoutée : #1 Acheter du pain
Ajoutée : #2 Lire un livre
Faite : #1
#1 [x] Acheter du pain
#2 [ ] Lire un livre
Résumé : 2 tâche(s), 1 faite(s)
```

## Conseils

- **Le port d'abord** : définis `IDepotTaches` dans Domain (Ajouter / Trouver / Lister), puis fais
  travailler `GestionTaches` avec **cette interface**, reçue par le constructeur.
- `DepotMemoire` implémente `IDepotTaches` avec une `List<Tache>` et un compteur d'id.
- Dans `Program`, **une seule ligne** crée l'implémentation concrète :
  `new GestionTaches(new DepotMemoire())`. Si tu te retrouves à écrire `new DepotMemoire()` ailleurs
  que dans `Program`, c'est que tu casses la règle de dépendance.
- Chaque fichier dans **son sous-dossier et son namespace** ; `using Domain;` explicite là où il faut.

## Livrables

- `Domain/Tache.cs`, `Domain/IDepotTaches.cs`
- `Application/GestionTaches.cs`
- `Infrastructure/DepotMemoire.cs`
- `Program.cs`
