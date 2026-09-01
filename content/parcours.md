# Parcours narratif — Centre Asteria

La piscine suit la remise en service progressive du **centre Asteria**. Le récit donne un but aux
exercices sans masquer l'objectif technique : chaque module annonce une mission, chaque exercice un
jalon, et les Rushes vérifient que plusieurs capacités fonctionnent ensemble.

## Les sept actes

| Acte | Modules | Intention pédagogique | Situation Asteria |
|---|---:|---|---|
| I — Prise de quart | M00–M05 | environnement, bases C#, boucles, méthodes, données et premier workflow Git | reprendre le contrôle d'un poste isolé |
| II — Cœur métier | M06–M14 | collections, objet, erreurs, génériques, LINQ, async, tests et collaboration | reconstruire les règles métier et leur filet de sécurité |
| III — Services en production | M15–M23 | texte/JSON, réflexion, DI, logs, Generic Host, concurrence, réseau et patterns | transformer les programmes en services exploitables |
| IV — Algorithmes et langage avancé | M24–M33 | patterns C#, données binaires, complexité, graphes, conception et refactoring | optimiser les mécanismes critiques sans perdre leur lisibilité |
| V — Frontières et interfaces | M34–M39 | interop, EF Core, Clean Architecture, Docker, GPU et Blazor | reconnecter stockage, natif, livraison et interfaces opérateur |
| VI — Asteria distribué | M40–M43 | brokers, NATS, résilience, observabilité et Aspire | réunifier les services et prouver leur reprise après panne |
| VII — Applications d'entreprise hybrides | M44–M52 | socle projet, desktop/MVVM, façades HTTP, identité, données, gRPC, tests, CI/CD et diagnostic | livrer un produit où le poste lourd reste central et s'appuie sur des services distribués maîtrisés |

Les prérequis sont des conseils de navigation, pas des verrous. Un apprenant expérimenté peut sauter
une étape puis revenir au concept qui lui manque.

## Rythme d'un module

1. **Comprendre** : lire le cours et reformuler l'invariant principal.
2. **Découvrir** : exercice facile, 15 à 35 minutes, une notion dominante.
3. **Appliquer** : exercices moyens, généralement 35 à 75 minutes, contexte ou données différents du
   cours.
4. **Combiner** : exercice difficile ou bonus, 60 à 120 minutes, plusieurs choix et cas limites.
5. **Prouver** : lancer la correction, lire le diagnostic, corriger puis expliquer pourquoi le cas
   manquant échouait.

`estimated_minutes` mesure du travail actif après lecture. Ce n'est ni une limite ni une promesse :
les premières rencontres avec Git, un IDE ou un domaine peuvent demander plus de temps.

## Missions de synthèse

| Rush | Après | Concepts regroupés |
|---|---|---|
| R0 — FizzBuzz | M04 | conditions, boucles, méthodes et format de sortie |
| R1 — Inventaire | M08 | collections, modèle objet et commandes |
| R2 — Rapport | M12 | parsing, LINQ, agrégation et async |
| R3 — Traitement | M23 | Generic Host, DI, Channel, logging et arrêt propre |
| R4 — Clean Architecture | M36 | domaine, ports, adaptateurs et règle de dépendance |
| R5 — Event processor | M42 | JSON, async, architecture, inbox idempotente et audit |
| R6 — Asteria distribué | M43 | Aspire, NATS JetStream, health checks, OTel et runbook de reprise |
| R7 — Poste d'entreprise | M52 | client lourd réactif, API sécurisée, gRPC, EF/outbox, tests, CI et exploitation |

Un Rush ne présente pas de nouveau cours. Il doit être abordé comme un mini-projet : découper,
implémenter, vérifier chaque risque puis exécuter le scénario complet.

## Progression et gamification

- Les **XP** sont attribués une seule fois, à la première réussite. Un nouvel essai ne peut ni les
  doubler ni les retirer.
- Les niveaux utilisent des seuils progressifs : niveau 2 à 250 XP, niveau 3 à 750, niveau 4 à
  1 500, niveau 5 à 2 500. Le tableau de bord montre la progression vers le suivant.
- La **série** compte les jours de pratique consécutifs et tolère que la dernière pratique date
  d'hier ; elle encourage le retour, pas la course au volume.
- Les badges marquent un premier succès, des paliers d'exercices, un bonus réussi du premier coup et
  des missions Git, architecture, messaging ou capstone.

La gamification reste informative : aucun contenu n'est verrouillé et aucun classement entre
personnes n'est produit.

## Contrat anti-copie

Le cours peut montrer une API ou un exemple, mais l'exercice change au moins le domaine, les données,
la composition ou la décision. La sortie seule est complétée par des graders structurels lorsque la
technique fait partie de l'objectif : `source`, `mutation`, `projet`, `git` ou `fichier`. Les indices
progressent de l'invariant vers la structure puis la syntaxe, sans donner l'implémentation complète.
