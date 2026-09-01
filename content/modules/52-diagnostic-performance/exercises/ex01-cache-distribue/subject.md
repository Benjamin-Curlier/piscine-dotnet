# ex01 — Cache-aside explicite

Implémente `CachedCommandReader` avec `HybridCache`. La clé inclut l'identifiant, la factory charge le
repository en asynchrone, et le token est propagé. Configure une expiration distribuée de 5 minutes,
une expiration locale plus courte et un tag `commands`.

Ajoute une métrique `cache.requests`. N'utilise ni dictionnaire statique, ni expiration infinie, ni
blocage synchrone.
