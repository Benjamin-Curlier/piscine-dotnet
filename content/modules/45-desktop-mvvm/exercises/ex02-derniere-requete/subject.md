# ex02 — Latest wins

Implémente `SearchAsync` : chaque invocation annule la précédente, capture un numéro croissant, attend
le client de recherche puis ne publie le résultat que si sa révision est encore la plus récente.

Traite l'annulation attendue sans masquer les autres exceptions. Les blocages synchrones et
`async void` sont interdits.
