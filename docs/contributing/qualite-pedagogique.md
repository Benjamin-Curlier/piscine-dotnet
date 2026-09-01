# Qualité pédagogique du contenu

## Une notion, puis une adaptation

Le cours explique le concept et peut montrer un exemple, mais l'exercice doit changer au moins un
axe : domaine, données, composition de plusieurs notions ou choix de conception. Une solution qui se
résume à copier une ligne du cours doit être transformée en découverte assumée et facile, ou enrichie.

## Indices progressifs

Présenter les indices dans cet ordre :

1. rappeler l'invariant ou la question à se poser ;
2. proposer la structure de l'algorithme ou un pseudo-code ;
3. donner une API ou un détail syntaxique, sans recopier l'implémentation complète.

Les contraintes importantes doivent être vérifiées. Le grader `source` accepte des
`required_fragments`, `forbidden_fragments` et `required_occurrences` lorsque le résultat seul ne
prouve pas la technique :

```yaml
- type: source
  source:
    required_fragments: ["% 2"]
    forbidden_fragments: ["Convert.ToString"]
    required_occurrences:
      - { fragment: "Handle(request)", count: 3 }
```

Pour un livrable texte qui ne doit pas être compilé par Roslyn (`Dockerfile`, `.razor`, shader,
AppHost isolé), le grader `fichier` vérifie le contrat minimal. Le sujet doit alors demander une
validation réelle complémentaire si le runtime, le navigateur, le GPU ou le broker apporte une
sémantique que l'inspection ne peut pas prouver.

## Calibrage

- `facile` : une notion dominante, généralement 15–35 min ;
- `moyen` : adaptation et cas limites, généralement 35–75 min ;
- `difficile` : composition ou conception, généralement 60–120 min et au moins deux paliers d'indice.

Chaque manifest déclare aussi des XP (environ deux points par minute), des tags et un `story_beat`.
Les XP récompensent la première réussite mais ne modifient jamais l'autorité des graders.

Un Rush déclare en plus le module `recommended_after`. Ce jalon alimente la frise et la recommandation
de l'application, mais ne verrouille jamais la mission. Utiliser `manual_validation: true` quand un
runbook, une coupure réelle, une preuve visuelle ou un environnement externe doit faire l'objet d'une
auto-relecture guidée après les contrôles déterministes. Cette attestation locale soutient la
progression personnelle ; elle ne constitue pas une certification ni une revue externe.

## Validation auteur

```bash
piscine validate-content  # contrat bloquant : manifests, starters, corrigés et graders
piscine audit-content     # avis : fuite cours/indice/starter, module vide, durée/difficulté, similarité
```

L'audit déterministe doit être traité avant toute revue IA. Une IA locale peut ensuite relire cours
et sujets pour proposer des reformulations, à condition de ne recevoir aucune donnée de recrue et de
rester consultative : pas de modification automatique, pas de verdict de correction, pas de gate CI.
