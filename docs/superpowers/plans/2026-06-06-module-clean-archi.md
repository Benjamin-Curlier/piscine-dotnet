# Sprint 4 (V3) — Module Clean Architecture (issue #5)

> Scrum / loop V3. Branche `feat/module-clean-archi`. Premier **contenu `projet`** (consomme #4).

## Objectif
Module `36-clean-architecture` : enseigner les **couches** et la **règle de dépendance**
(Domain ne dépend de rien ; Application dépend de Domain ; Infrastructure implémente les ports ;
la composition root câble le tout). Auto-noté par le grader **`projet`** : cas io (comportement)
+ assertions d'architecture (`requires_types`, `forbidden_dependencies`).

## Exercice pilote `ex00-couches` — gestionnaire de tâches en couches
- **Domain** : `Tache` (entité), `IDepotTaches` (port). Ne dépend de rien.
- **Application** : `GestionTaches` (cas d'usage), dépend de `IDepotTaches` (abstraction Domain).
- **Infrastructure** : `DepotMemoire : IDepotTaches` (adaptateur en mémoire).
- **Program.cs** : composition root + entrée (lit des commandes, écrit la sortie).
- Livrables (sous-chemins) : `Domain/Tache.cs`, `Domain/IDepotTaches.cs`,
  `Application/GestionTaches.cs`, `Infrastructure/DepotMemoire.cs`, `Program.cs`.

### Notation
```yaml
- type: projet
  cases: [ ... io ... ]
  project:
    requires_types: [Domain.Tache, Domain.IDepotTaches, Application.GestionTaches, Infrastructure.DepotMemoire]
    forbidden_dependencies:
      - { from: Domain, to: Application }
      - { from: Domain, to: Infrastructure }
      - { from: Application, to: Infrastructure }
```
Commandes io : `add <titre>`, `done <id>`, `list`, puis résumé final.

## Méthode
1. Corrigé en couches d'abord. 2. Cas io (`expect_stdout: "?"`). 3. `try --write`. 4. `validate-content`.
5. commit/push séparés, PR, CI, merge. (Aucun changement `src/`.)

## DoD (issue #5)
- [ ] Module `36-clean-architecture` (module.yaml + cours.md ancré)
- [ ] `ex00-couches` (manifest `projet` + subject + starter + solution multi-couches)
- [ ] expects générés par `try --write` ; `validate-content` vert + CI verte + PR mergée
- [ ] Rush dédié = noté en follow-up si le temps manque (descope scrum assumé)
