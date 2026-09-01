# Module 52 — Diagnostic et performance

« C'est lent » n'est pas un diagnostic. Commence par définir le symptôme, reproduire, mesurer et
conserver une trace avant de modifier le code.

## 1. Du symptôme au signal {#diagnostic}

- UI figée : durée des handlers, file du dispatcher, thread bloqué ;
- CPU élevé : profil d'échantillonnage et stacks chaudes ;
- mémoire croissante : taille du heap, allocations, racines et dump ;
- requêtes lentes : trace distribuée, SQL et plan ;
- traitement en retard : profondeur de file, âge du message et débit ;
- contention : verrous, ThreadPool et temps d'attente.

`dotnet-counters` donne une première vue live. `dotnet-trace` capture CPU et événements. `dotnet-dump`
permet l'analyse mémoire et des threads. Les profilers IDE complètent ces outils. Conserve version,
charge et environnement avec la mesure.

## 2. Budgets {#budgets}

Fixe des budgets observables : temps de démarrage du poste, absence de gel UI supérieur à un seuil,
p95 de commande, mémoire stable après un cycle, backlog maximal et temps de reprise. Compare à une
baseline ; un microbenchmark ne prouve pas la latence du système complet.

## 3. Cache {#cache}

Un cache échange cohérence contre latence ou charge. Définis clé, durée, taille, invalidation et
comportement en panne. Un cache local est propre à un poste ou processus ; un cache distribué partage
l'état entre instances mais devient une dépendance réseau.

Le pattern cache-aside lit le cache, charge la source puis remplit. Préviens le stampede lorsqu'une clé
expire. Ne cache jamais une autorisation au-delà de sa durée de validité. Pour le desktop hors ligne,
distingue cache d'affichage, source de vérité et file de commandes à synchroniser.

## 4. Optimiser avec preuve {#optimiser}

Réduis d'abord I/O, requêtes inutiles, allocations massives et sérialisations répétées. `Span<T>` ou
pooling ne viennent qu'après mesure, car ils augmentent la complexité. Rejoue exactement la même
charge et vérifie que la correction n'a pas déplacé le coût.

### Atelier réel

Lance un worker sous charge, observe compteurs runtime et backlog, collecte une trace, introduis un
cache avec métriques hit/miss puis compare la baseline. Répète en coupant le cache.

Références :

- [Outils de diagnostic .NET](https://learn.microsoft.com/dotnet/core/diagnostics/)
- [Collecter des métriques avec dotnet-counters](https://learn.microsoft.com/dotnet/core/diagnostics/metrics-collection)
- [Cache ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/caching/overview)
