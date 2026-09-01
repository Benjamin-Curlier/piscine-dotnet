# Module 41 — NATS avec .NET 10

NATS fournit un serveur compact et un protocole orienté sujets. **Core NATS** privilégie les échanges
éphémères et rapides ; **JetStream** ajoute persistance, consommateurs, accusés de réception et rejeu.
Le client .NET moderne est `NATS.Net` (API asynchrone).

Les exercices simulent les règles déterministes. L'atelier final les confronte à un vrai serveur :
une correction locale ne doit jamais devenir verte uniquement parce qu'un broker oublié tourne sur
la machine.

## 1. Connexion, publication et abonnement {#core}

Un fichier C# .NET 10 peut épingler le paquet et lancer un premier échange :

```csharp
#:package NATS.Net@3.2.0
using NATS.Client.Core;

await using var nats = new NatsClient();
await nats.PublishAsync("asteria.orders.created", new OrderCreated(42));

await foreach (var message in nats.SubscribeAsync<OrderCreated>("asteria.orders.*"))
{
    Console.WriteLine(message.Data?.Id);
}

record OrderCreated(int Id);
```

Une connexion est une ressource longue durée : partage-la plutôt que d'en créer une par message.
Tous les appels réseau sont asynchrones et doivent recevoir un `CancellationToken` dans un service
réel. Le type sérialisé est un **contrat de fil** ; fais évoluer ce contrat de manière compatible.

## 2. Queue groups {#queue-groups}

Plusieurs abonnés au même sujet reçoivent chacun une copie. S'ils utilisent aussi le même **queue
group**, un seul membre du groupe reçoit chaque message : on scale horizontalement un worker sans
multiplier les effets métier.

```csharp
await foreach (var msg in nats.SubscribeAsync<Job>("jobs.render", queueGroup: "renderers"))
{
    await RenderAsync(msg.Data!, cancellationToken);
}
```

Un queue group Core NATS ne rend pas le message durable. Si aucun membre n'est connecté, le message
n'est pas conservé. Pour une file durable, utilise JetStream et configure rétention, consommateurs et
limites.

## 3. Request/reply et corrélation {#request-reply}

NATS implémente request/reply avec un sujet de réponse unique (*inbox*). Le client corrèle la réponse
à la requête, mais ton code doit encore borner l'attente :

```csharp
using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
timeout.CancelAfter(TimeSpan.FromSeconds(2));
var reply = await nats.RequestAsync<Question, Answer>("asteria.lookup", question,
    cancellationToken: timeout.Token);
```

Une réponse tardive ne doit jamais satisfaire la mauvaise requête. Propager un `correlationId` dans
le message, les logs et les traces simplifie le diagnostic au-delà de l'inbox technique.

## 4. JetStream : stream et consumer {#jetstream}

Un **stream** capture un ou plusieurs subjects et définit stockage, rétention, réplication et limites.
Un **consumer** conserve une position de lecture et une politique d'accusé. Ce sont deux cycles de vie
différents : plusieurs consumers peuvent lire le même stream à leur rythme.

- `ACK` : traitement terminé, cette livraison peut être considérée acquittée ;
- `NAK` : traitement échoué, demander une nouvelle livraison ;
- absence d'ACK avant `AckWait` : redélivrance ;
- `MaxDeliver` : borne les tentatives avant parking ou traitement d'erreur.

Accuse **après** la persistance de l'effet. Comme un ACK peut se perdre, l'idempotence reste requise.
Les séquences du stream aident au diagnostic mais un identifiant métier stable reste préférable pour
dédupliquer un effet.

## 5. Key/Value, Object Store et Services {#ecosysteme}

JetStream propose aussi un magasin Key/Value versionné et un Object Store pour les blobs. Ce ne sont
pas des remplacements universels d'une base métier : utilise-les lorsque leur modèle de cohérence et
de rétention correspond au besoin. Le framework NATS Services formalise endpoints, statistiques et
découverte autour de request/reply.

## 6. Atelier avec un vrai serveur {#atelier}

1. Lance `nats-server -js` puis vérifie sa disponibilité.
2. Crée un publisher et deux subscribers ordinaires : observe la diffusion.
3. Place les subscribers dans le même queue group : observe la répartition.
4. Crée un stream, arrête le consumer, publie, puis redémarre-le : prouve la reprise.
5. Fais échouer un traitement avant ACK : observe la redélivrance et vérifie l'idempotence.
6. Coupe puis redémarre le serveur : distingue reconnexion du client et récupération des données.

## Références

- [NATS .NET](https://github.com/nats-io/nats.net)
- [NATS — concepts et JetStream](https://docs.nats.io/)
