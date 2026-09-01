# ex00 — Simuler un queue group

Lis le nombre de workers, le nombre de messages, puis un message par ligne. Répartis-les en
round-robin et affiche `worker-N message`. Un message doit être livré à un seul worker.

Cette simulation isole la sémantique d'un queue group ; le serveur NATS choisit réellement les
membres disponibles et ne promet pas ce round-robin exact.
