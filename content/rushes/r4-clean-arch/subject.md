# Rush 4 — Catalogue de produits (Clean Architecture)

> **Un Rush est un projet de synthèse solo.** Pas de nouveau cours : tu réutilises la **règle de
> dépendance** et le découpage en couches vus en **M36 (Clean Architecture)**.

## Le contexte

Tu construis un **catalogue de produits** en respectant la **Clean Architecture**. L'enjeu n'est pas
seulement que le programme produise la bonne sortie : c'est que les **dépendances entre couches**
respectent la **règle de dépendance** (les dépendances pointent toujours *vers l'intérieur*, du
concret vers l'abstrait).

La moulinette compile **tous les fichiers ensemble** et, en plus de comparer la sortie, **inspecte
les dépendances entre namespaces** (analyse Roslyn). Une couche qui dépend d'une couche plus externe
fait **échouer** la correction, même si la sortie est correcte.

## Les couches

| Couche | Namespace | Sous-dossier | Rôle |
|---|---|---|---|
| Domaine | `Domain` | `Domain/` | entité `Produit` + **port** `ICatalogueProduits`. Pur métier, aucune dépendance technique. |
| Application | `Application` | `Application/` | cas d'usage `GestionCatalogue`. Dépend **uniquement** du port (Domain). |
| Infrastructure | `Infrastructure` | `Infrastructure/` | adaptateur `CatalogueMemoire` qui **implémente** le port. Dépend de Domain. |
| Composition root | *(top-level)* | `Program.cs` | le **seul** endroit qui connaît les implémentations concrètes et les câble. |

### La règle de dépendance (notée)

- `Domain` ne référence **ni** `Application` **ni** `Infrastructure`.
- `Application` dépend **seulement** du port `ICatalogueProduits` (dans `Domain`), **jamais** de
  `CatalogueMemoire` (Infrastructure).
- Seul `Program` (composition root) instancie `CatalogueMemoire` et l'injecte dans `GestionCatalogue`.

## Le problème

Lis **un entier** `N` sur l'entrée standard, puis lis `N` lignes de commandes. Les champs sont
séparés par **un espace**. Les commandes :

| Commande | Effet | Sortie |
|---|---|---|
| `add <nom> <prix>` | ajoute un produit | `Ajouté : #<id> <nom> (<prix>)` |
| `price <id> <prix>` | met à jour le prix | `Mis à jour : #<id>` si trouvé, sinon `Inconnu : #<id>` |
| `get <id>` | affiche un produit | `#<id> <nom> (<prix>)` si trouvé, sinon `Inconnu : #<id>` |
| `list` | liste tout le catalogue | une ligne `#<id> <nom> (<prix>)` par produit |

À la fin (après les `N` commandes), affiche **toujours** :

```
Catalogue : <total> produit(s)
```

- Les **id** sont attribués automatiquement, à partir de `1`, dans l'ordre des `add`.
- Le **prix** est un nombre décimal, **toujours affiché avec 2 décimales** (point décimal, p. ex.
  `1.50`, `0.99`). Utilise le **point** comme séparateur en lecture **et** en écriture
  (`CultureInfo.InvariantCulture`).

### Exemple

Entrée :

```
3
add Pain 1.50
add Lait 0.99
list
```

Sortie :

```
Ajouté : #1 Pain (1.50)
Ajouté : #2 Lait (0.99)
#1 Pain (1.50)
#2 Lait (0.99)
Catalogue : 2 produit(s)
```

## Livrables

- `Domain/Produit.cs` — l'entité métier.
- `Domain/ICatalogueProduits.cs` — le port (interface) défini par le domaine.
- `Application/GestionCatalogue.cs` — le cas d'usage (reçoit le port par injection).
- `Infrastructure/CatalogueMemoire.cs` — l'adaptateur en mémoire qui implémente le port.
- `Program.cs` — la composition root (câblage + lecture/écriture).

## Conseils

- **Chaque couche dans son namespace et son sous-dossier.** Mets des `using` **explicites** :
  `Application` et `Infrastructure` font `using Domain;`.
- **Instructions top-level d'abord** dans `Program.cs`, puis les types **après** (si tu en ajoutes).
- `GestionCatalogue` reçoit un `ICatalogueProduits` par **constructeur** — ne référence **jamais**
  `CatalogueMemoire` depuis `Application`.
- Pour `Trouver`, retourne `null` si l'id est absent (le cas d'usage gère le `Inconnu`).
- Pour le prix : `decimal.Parse(..., CultureInfo.InvariantCulture)` en lecture,
  `$"{prix:F2}"` (ou `prix.ToString("F2", CultureInfo.InvariantCulture)`) en écriture.

## Rendu

Comme pour un exercice : travaille dans ton workspace, puis `git add` / `commit` / `push origin main`.
La moulinette corrige le Rush comme un livrable autonome : sortie **et** règle de dépendance.
