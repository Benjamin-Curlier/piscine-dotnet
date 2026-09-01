# Module 40 — Brokers et messaging

Quand deux services s'appellent directement, chacun doit connaître l'adresse et la disponibilité de
l'autre. La messagerie introduit une frontière : un producteur publie un **message**, un consommateur
le traite, et leur coordination dépend d'un protocole ou d'un intermédiaire plutôt que d'un appel de
méthode. Ce découplage aide à absorber les pointes, diffuser des événements et tolérer des pannes.

## 1. Les mots à ne pas confondre {#vocabulaire}

- Un **message** porte une intention ou un fait, avec un contrat versionné.
- Une **commande** demande une action à un destinataire logique : `Facture.AEmettre`.
- Un **événement** décrit un fait passé : `FactureEmise`.
- Une **file de travail** distribue chaque message à un seul membre d'un groupe.
- Le **publish/subscribe** diffuse un événement à plusieurs abonnés indépendants.
- Le **request/reply** demande une réponse, mais reste un échange réseau : timeout et annulation sont
  obligatoires.
- Un **stream** conserve des messages pour permettre accusés de réception, reprise et rejeu.

Un broker ne transforme pas automatiquement un système en architecture événementielle. Le contrat,
l'idempotence, l'observabilité et la stratégie d'échec restent à concevoir.

## 2. Trois familles, trois compromis {#comparaison}

| Technologie | Topologie | Point fort | Vigilance |
|---|---|---|---|
| **NATS** | serveur léger ; Core NATS éphémère, JetStream persistant | faible latence, sujets simples, request/reply, services et streams dans un même écosystème | Core NATS ne conserve pas les messages ; choisir explicitement JetStream si la reprise compte |
| **RabbitMQ** | broker durable centré sur files et exchanges | routage riche, files de travail, accusés de réception et écosystème AMQP | davantage de topologie à administrer ; l'ordre et la redélivrance dépendent de la configuration |
| **ZeroMQ** | bibliothèque **sans broker** entre pairs | transport très léger, topologies embarquées et contrôle fin | pas de stockage ou d'exploitation centralisée fourni ; l'application porte davantage de responsabilités |

Le choix ne se fait pas à la popularité. Pars des contraintes : un message doit-il survivre à une
panne ? être rejoué ? diffusé ? traité une seule fois par un groupe ? fonctionner sans service
central ? Quelle latence, quel débit et quelle équipe d'exploitation ?

## 3. Adressage et routage {#routage}

NATS adresse les messages par **subjects** hiérarchiques séparés par des points :
`asteria.capteurs.temperature`. Un abonnement peut utiliser `*` pour exactement un segment et `>`
pour la fin du sujet :

```text
asteria.*.temperature   # un segment variable
asteria.>               # tout le sous-arbre
```

RabbitMQ sépare généralement l'**exchange** de la **queue**. Un binding relie les deux selon le type
d'exchange (`direct`, `topic`, `fanout`, `headers`). ZeroMQ compose plutôt des types de sockets
(`PUB/SUB`, `PUSH/PULL`, `REQ/REP`, `ROUTER/DEALER`) directement dans les processus.

Nommer par intention métier (`orders.created.v1`) est plus durable que par nom d'application ou de
machine. Un contrat partagé doit définir schéma, version, identifiant, date, corrélation et règles de
compatibilité.

## 4. Livraison : les mots honnêtes {#livraison}

- **Au plus une fois** : pas de redélivrance ; un message peut être perdu.
- **Au moins une fois** : redélivrance possible ; un message peut être traité plusieurs fois.
- **Exactement une fois** : propriété de bout en bout très coûteuse et souvent limitée à un périmètre.

Dans la pratique, vise une livraison au moins une fois avec un consommateur **idempotent** : chaque
message possède un identifiant stable, et le consommateur mémorise les identifiants déjà appliqués
dans la même transaction que son effet métier. Accuser réception avant d'avoir persisté l'effet peut
perdre le message ; accuser après peut provoquer une répétition.

L'**outbox transactionnelle** enregistre l'état métier et l'événement à publier dans la même base.
Un relais publie ensuite l'outbox. L'**inbox** joue le rôle symétrique côté consommation.

## 5. Échecs et exploitation {#echecs}

Prévois dès le contrat :

1. timeout et annulation pour toute attente ;
2. retry borné avec délai et jitter uniquement sur les erreurs transitoires ;
3. dead-letter ou parking pour les messages définitivement invalides ;
4. identifiant de corrélation propagé dans logs et traces ;
5. métriques de débit, latence, âge de file, redélivrance et échec ;
6. backpressure : ralentir ou borner plutôt que saturer la mémoire.

## 6. Atelier réel conseillé {#atelier-reel}

Après les exercices déterministes, démarre séparément NATS, RabbitMQ puis un exemple NetMQ. Pour
chacun, reproduis publication, abonnement, arrêt du consommateur, reprise et doublon. Note ce qui est
fourni par le produit et ce que ton code doit assurer. Les modules suivants approfondissent NATS et
l'orchestration locale avec Aspire.

## Références

- [Documentation NATS](https://docs.nats.io/)
- [Tutoriels RabbitMQ](https://www.rabbitmq.com/tutorials)
- [ZeroMQ — démarrage](https://zeromq.org/get-started/)
