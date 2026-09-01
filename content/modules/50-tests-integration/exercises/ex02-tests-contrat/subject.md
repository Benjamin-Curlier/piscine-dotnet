# ex02 — Gate de compatibilité

Écris un job GitLab CI `contracts` exécuté sur les Merge Requests. Il lance une comparaison Protobuf
avec `buf breaking` et une comparaison OpenAPI avec `oasdiff breaking`, entre la baseline versionnée
et le document généré dans les artefacts.

Le job est bloquant, conserve ses artefacts même en cas d'échec et n'utilise aucun tag `latest`.
