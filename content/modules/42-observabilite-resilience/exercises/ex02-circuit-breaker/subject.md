# ex02 — Circuit breaker

Lis `failureThreshold cooldown`, puis `N` résultats (`OK` ou `FAIL`). Le circuit commence `Closed`.

- En Closed : `OK` affiche `OK` et remet les échecs consécutifs à zéro. `FAIL` affiche `FAIL`, sauf
  celui qui atteint le seuil : il affiche `OPEN` et ouvre le circuit.
- En Open : saute exactement `cooldown` événements en affichant `SKIP`.
- L'événement suivant est la sonde half-open : `OK` affiche `PROBE_OK` et ferme ; `FAIL` affiche
  `PROBE_FAIL`, rouvre et redémarre le cooldown.

Affiche enfin `STATE CLOSED` ou `STATE OPEN`. Modélise l'état avec `enum CircuitState`.
