# Module 48 — Données de production

SQLite en mémoire a permis d'apprendre EF Core. Une base d'entreprise ajoute durée de vie, volume,
concurrence, sauvegarde, droits séparés et évolution du schéma.

## 1. SQL que le développeur doit comprendre {#sql}

Même avec un ORM, lis le SQL produit. Maîtrise clés primaires/étrangères, `JOIN`, contraintes,
transactions et index. Un index accélère certaines lectures mais coûte en écriture et en stockage.
Mesure sur une requête réelle avant d'en ajouter.

Évite le N+1 : projette seulement les colonnes utiles, observe les requêtes et choisis explicitement
chargement joint, séparé ou différé. `AsNoTracking` convient aux lectures qui ne seront pas modifiées.

## 2. Migrations {#migrations}

Une migration est du code versionné. Relis le SQL généré, teste montée et retour, sauvegarde avant une
opération risquée. Pour la production, préfère un script relu ou un bundle appliqué par une identité
de déploiement. L'identité runtime ne devrait pas posséder le droit de modifier le schéma.

Ne mélange pas `EnsureCreated` et migrations sur une base durable. La CI peut détecter un modèle qui
n'a pas de migration correspondante.

## 3. Transactions et concurrence {#concurrence}

Une transaction protège un invariant sur plusieurs écritures. Elle doit être courte et ne pas inclure
un appel réseau lent. Un message publié après commit exige un pattern comme l'outbox si l'effet base
et l'événement doivent rester cohérents.

La concurrence optimiste associe une version à la ligne. La mise à jour n'aboutit que si la version
lue est encore actuelle. En cas de conflit, recharge, fusionne selon le métier ou demande à
l'opérateur ; n'écrase pas silencieusement.

## 4. Pagination et annulation {#requetes}

Une API ne charge pas toute une table. Filtre et trie de manière déterministe avant pagination. Pour
les grands volumes, une continuation fondée sur la dernière clé est souvent plus stable que `Skip`.
Toutes les opérations asynchrones reçoivent le `CancellationToken` de la requête.

### Atelier réel

Utilise PostgreSQL ou SQL Server local, applique une migration, lance deux modifications concurrentes
du même enregistrement, puis inspecte la requête paginée et son plan d'exécution.

Références :

- [Migrations EF Core](https://learn.microsoft.com/ef/core/managing-schemas/migrations/)
- [Appliquer les migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying)
