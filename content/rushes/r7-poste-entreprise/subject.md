# Rush 7 — Poste Asteria d'entreprise

Tu livres le dossier de conception exécutable d'un produit hybride : une application lourde reste
l'outil principal de l'opérateur, tandis qu'une API et un flux gRPC relient les services distribués.
La commande est persistée avec idempotence et outbox avant publication NATS.

## Socle

`Directory.Build.props` impose .NET 10, nullable, warnings bloquants et build déterministe.

## Client lourd

`DesktopViewModel.cs` expose un état observable, dépend de `ICommandGateway` et `IUiDispatcher`, annule
l'envoi précédent, protège contre un résultat obsolète et ne bloque jamais le thread UI.

## Façade et flux

`Api.cs` configure ProblemDetails, OpenAPI, authentification/autorisation et `POST /commands`. La route
exige la policy `send-command`, une clé `Idempotency-Key`, propage l'annulation et répond 202.
Expose aussi `public partial class Program` afin que le pipeline puisse être démarré par
`WebApplicationFactory<Program>` depuis le projet de tests.

`commands.proto` publie le streaming des statuts dans un package v1 et réserve un ancien champ.

## Données et messaging

`CommandStore.cs` impose l'unicité de la clé d'idempotence et une version de concurrence. Une
transaction enregistre commande et `OutboxMessage`, puis commit ; aucun message n'est publié avant le
commit.

## Preuves

`IntegrationTests.cs` utilise `WebApplicationFactory` pour vérifier 202, 401 et le même identifiant
lors du rejeu de la clé. `.gitlab-ci.yml` restaure en mode verrouillé, compile, teste, formate, audite
les paquets et conserve les artefacts.

`OPERATIONS.md` décrit nominal, poste déconnecté, coupures API/NATS, conflit de données, coexistence
ancien/nouveau client et rollback. Pour chaque scénario : action, signal, critère de réussite.

La moulinette inspecte les huit contrats. La recette finale reste réelle : branche les fichiers dans
une solution Avalonia ou WPF, une API ASP.NET Core, EF Core, NATS et Aspire ; exécute tests et runbook
sur ton environnement.

## Auto-relecture

Après les contrôles automatiques, relis le runbook puis rejoue au minimum le nominal, une déconnexion
du poste, une coupure d'API ou de NATS et un rejeu de clé d'idempotence. Vérifie la réactivité du
client, la reprise, l'outbox et l'absence de double effet ; conserve une référence locale vers les
preuves que tu juges utiles.

```text
piscine review complete r7-poste-entreprise --evidence <référence-locale> --attest
```

Tu attestes ton propre travail pour progresser. La commande ne délivre aucun certificat et ne fait
intervenir aucun correcteur externe.
