# Module 42 — Observabilité et résilience

Un système distribué échoue rarement de manière binaire. Une dépendance ralentit, une réponse arrive
après le timeout, un message est redélivré, ou une instance seulement devient indisponible. La
résilience borne l'impact ; l'observabilité permet de comprendre ce qui s'est réellement produit.

## 1. Trois signaux complémentaires {#signaux}

- **Logs** : événements discrets et structurés (`orderId`, `correlationId`, niveau, exception).
- **Metrics** : séries numériques agrégées, adaptées aux alertes et tendances (débit, erreurs,
  saturation, percentiles de latence).
- **Traces** : parcours d'une opération à travers plusieurs services ; une trace contient des spans
  parent/enfant et leurs durées.

OpenTelemetry fournit des API et conventions communes pour produire et exporter ces signaux. Il ne
remplace ni le backend d'analyse ni une stratégie de nommage. Une information utile doit être
structurée, peu cardinalisée pour les métriques, et corrélable d'un service à l'autre.

## 2. Corrélation et contexte {#correlation}

Un `traceId` relie les spans d'une même opération. Un identifiant métier (`orderId`) répond à une
autre question et doit souvent être conservé en attribut. Propage le contexte via les en-têtes HTTP
et les métadonnées de message ; ne crée pas une nouvelle trace sans lien à chaque saut.

Pour chaque frontière, mesure au minimum : nom de l'opération, durée, résultat, dépendance appelée et
tentatives. N'insère pas de secret, contenu personnel ou payload volumineux dans les attributs.

## 3. Timeout et annulation avant retry {#timeout}

Sans timeout, une attente peut immobiliser toutes les ressources. Sans propagation du
`CancellationToken`, l'appel interne continue alors que le client est parti. Le budget global doit
être partagé entre les étapes : trois retries de cinq secondes ne respectent pas un budget de cinq
secondes.

Un **retry** ne convient qu'à une erreur transitoire et une opération sûre à répéter. Borne les
tentatives, ajoute un backoff avec jitter et observe chaque tentative. Ne retry jamais une validation
métier, une authentification refusée ou une opération non idempotente sans clé de déduplication.

## 4. Circuit breaker {#circuit-breaker}

Après plusieurs échecs, continuer à appeler une dépendance augmente sa saturation. Le circuit passe :

1. **Closed** : les appels passent ; les échecs sont comptés.
2. **Open** : les appels sont refusés rapidement pendant une durée de récupération.
3. **Half-open** : un petit nombre d'appels sonde la dépendance ; succès ferme, échec rouvre.

Le circuit breaker ne répare rien et ne remplace pas le timeout. Son état doit être observable. Une
réponse de repli (*fallback*) n'est acceptable que si le métier tolère des données absentes ou
périmées.

## 5. Backpressure, bulkhead et santé {#degradation}

- **Backpressure** : borne files et concurrence ; refuse ou ralentis avant l'épuisement mémoire.
- **Bulkhead** : sépare les pools de ressources afin qu'une dépendance lente ne bloque pas tout.
- **Rate limiting** : protège une capacité limitée.
- **Health checks** : la liveness dit que le processus vit ; la readiness qu'il peut recevoir du
  trafic. Une dépendance facultative ne doit pas nécessairement rendre toute l'application unhealthy.

Chaque politique change le comportement métier. Documente qui attend, ce qui est perdu ou différé,
et comment l'opérateur voit l'état.

## 6. Mesurer ce que l'utilisateur ressent {#slo}

Un SLI mesure un comportement (proportion de succès, latence sous un seuil). Un SLO fixe la cible sur
une fenêtre. Évite les moyennes seules : le percentile 95 ou 99 révèle les utilisateurs les plus
lents. Une alerte utile consomme un budget d'erreur ou annonce une saturation prochaine, plutôt que
chaque exception isolée.

## Références

- Microsoft Learn — observabilité .NET et OpenTelemetry
- OpenTelemetry — concepts traces, metrics et logs
- Microsoft.Extensions.Resilience / Polly — politiques de résilience .NET
