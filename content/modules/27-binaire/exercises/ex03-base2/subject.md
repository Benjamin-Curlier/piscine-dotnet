# ex03-base2 — Représentation binaire (BONUS)

## Objectif

Lis un entier **n supérieur ou égal à zéro** (`n >= 0`). Affiche sa **représentation binaire**, sans zéros de tête.
Le cas particulier `0` s'affiche `0`.

Exemple : `5` → `101`, `10` → `1010`.

## Livrable

- `Base2.cs`

## Contraintes

- Construis la représentation avec `% 2` et `/ 2`.
- N'utilise ni `Convert.ToString(n, 2)` ni une API équivalente de conversion de base.

## Indices progressifs

### 1. Démarrer

Traite d'abord le cas particulier `n == 0`. Pour les autres valeurs, prépare une chaîne vide qui
recevra les bits.

### 2. Trouver le prochain bit

À chaque tour, `n % 2` vaut `0` ou `1`. Ce bit est celui de **droite** ; place-le donc avant les bits
déjà trouvés, puis remplace `n` par `n / 2`.

### 3. S'arrêter

La boucle se termine lorsque le quotient devient zéro. Affiche alors la chaîne accumulée.
