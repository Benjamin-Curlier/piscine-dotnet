# ex01 — Dépendance réelle et jetable

Implémente une fixture xUnit asynchrone fondée sur Testcontainers PostgreSQL. Épingle l'image
`postgres:18-alpine`, démarre le conteneur, construit le DbContext depuis sa chaîne dynamique puis
applique les migrations. La fixture libère le conteneur en fin de suite.

N'utilise ni `latest`, ni `localhost`, ni délai arbitraire, ni `EnsureCreated`.
