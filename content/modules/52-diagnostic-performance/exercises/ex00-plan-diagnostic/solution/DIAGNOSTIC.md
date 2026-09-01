# Plan de diagnostic

Conserver version, environnement, scénario, même charge et baseline avant chaque comparaison.

## Gel UI

Rejouer l'action et mesurer la durée des handlers ainsi que la file du dispatcher. Une stack bloquée
ou un travail CPU long sur le thread UI confirme l'hypothèse.

## CPU élevé

Confirmer le processus et le taux avec `dotnet-counters`, puis capturer 30 secondes avec
`dotnet-trace`. Les stacks chaudes doivent expliquer la majorité du coût avant modification.

## Mémoire croissante

Suivre heap, allocations et GC avec `dotnet-counters`. Capturer deux états avec `dotnet-dump` et
comparer types retenus et racines après un cycle identique.

## Backlog broker

Mesurer profondeur, âge du message, redelivery et débit par consumer. Corréler avec traces et version
du worker avant d'augmenter le parallélisme.

## Contention

Observer longueur du ThreadPool, verrous et temps d'attente. Une trace doit identifier la ressource et
les appelants concurrents ; valider ensuite avec la même charge.
