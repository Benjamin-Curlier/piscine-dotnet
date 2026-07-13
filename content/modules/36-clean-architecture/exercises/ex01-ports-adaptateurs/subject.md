# Bibliothèque : deux ports, deux adaptateurs

Au premier exercice, tu avais **un** port (la persistance). Ici, tu montes d'un cran : **deux** ports.
Une petite bibliothèque doit **persister** ses livres *et* **notifier** chaque emprunt/retour. Le point
clé reste le même — **la règle de dépendance** — mais tu vas devoir câbler **deux** implémentations
concrètes depuis la composition root.

## Les couches attendues

| Couche | Fichier | Rôle | Dépend de |
|---|---|---|---|
| Domain | `Domain/Livre.cs` | entité `Livre` (+ règle : pas de double emprunt) | rien |
| Domain | `Domain/IDepotLivres.cs` | port de persistance | rien |
| Domain | `Domain/INotificateur.cs` | port de notification | rien |
| Application | `Application/Bibliotheque.cs` | cas d'usage | Domain (les deux ports) |
| Infrastructure | `Infrastructure/DepotMemoire.cs` | adaptateur en mémoire | Domain |
| Infrastructure | `Infrastructure/NotificateurConsole.cs` | adaptateur console | Domain |
| (racine) | `Program.cs` | composition root + entrée | toutes |

> **Règle de dépendance** : `Domain` ne référence ni `Application` ni `Infrastructure` ; `Application`
> ne dépend **que des ports** (`IDepotLivres`, `INotificateur`), jamais de `DepotMemoire` ni de
> `NotificateurConsole`. Seul `Program` câble les implémentations concrètes. La moulinette **vérifie
> ces dépendances**.

## Le comportement

Lis un entier `N`, puis `N` lignes de commandes :

- `add <titre>` → ajoute un livre (id auto-incrémenté à partir de 1) et affiche `Ajouté : #<id> <titre>` ;
- `emprunt <titre>` → si le livre existe **et** n'est pas déjà emprunté : le marque emprunté et affiche
  `Emprunt OK : <titre>` ; sinon `Emprunt refusé : <titre>` ;
- `rendre <titre>` → si le livre existe **et** est emprunté : le rend disponible et affiche
  `Retour OK : <titre>` ; sinon `Retour refusé : <titre>` ;
- `list` → une ligne par livre, dans l'ordre d'ajout : `#<id> <titre> [emprunté]` ou
  `#<id> <titre> [disponible]`.

**La notification** : à chaque emprunt ou retour **réussi**, l'application demande au `INotificateur`
de prévenir. Le `NotificateurConsole` écrit alors, **avant** la ligne de résultat, exactement :

```
[notif] « <titre> » emprunté
```

(ou `rendu` pour un retour). Un refus ne notifie **rien**.

### Exemple

Entrée :

```
5
add Dune
emprunt Dune
emprunt Dune
rendre Dune
list
```

Sortie :

```
Ajouté : #1 Dune
[notif] « Dune » emprunté
Emprunt OK : Dune
Emprunt refusé : Dune
[notif] « Dune » rendu
Retour OK : Dune
#1 Dune [disponible]
```

## Conseils

- **Les ports d'abord** : `IDepotLivres` (Ajouter / Trouver / Lister) **et** `INotificateur` (Notifier)
  dans Domain. `Bibliotheque` reçoit **les deux** par son constructeur et ne travaille qu'avec ces
  interfaces.
- La **règle métier** (« on ne prête pas un livre déjà emprunté ») vit dans le cœur (entité +
  application), **pas** dans l'infrastructure.
- Dans `Program`, **une seule ligne** câble le tout :
  `new Bibliotheque(new DepotMemoire(), new NotificateurConsole())`. Si tu écris `new DepotMemoire()` ou
  `new NotificateurConsole()` ailleurs que dans `Program`, tu casses la règle de dépendance.
- Reporte-toi au cours, section [ports & adaptateurs](cours.md#ports-adaptateurs).

## Livrables

- `Domain/Livre.cs`, `Domain/IDepotLivres.cs`, `Domain/INotificateur.cs`
- `Application/Bibliotheque.cs`
- `Infrastructure/DepotMemoire.cs`, `Infrastructure/NotificateurConsole.cs`
- `Program.cs`
