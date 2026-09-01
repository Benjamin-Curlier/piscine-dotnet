# Module 51 — CI/CD et livraison hybride

Une application lourde et ses services ne se livrent pas de la même manière, mais doivent provenir
du même commit vérifié et conserver une matrice de compatibilité explicite.

## 1. Intégration continue {#ci}

La CI repart d'un checkout propre et du SDK épinglé. Elle restaure, compile en Release, exécute tests
unitaires puis intégration, vérifie format/analyse/vulnérabilités et produit les artefacts. Les jobs
partagent des artefacts immuables, pas un répertoire de travail implicite.

Une Merge Request ne doit pas pouvoir contourner la gate. Les secrets CI sont masqués et limités aux
branches/environnements autorisés. Les dépendances sont verrouillées ou contrôlées selon la politique
du produit.

## 2. Build déterministe et version {#determinisme}

Un artefact porte version sémantique, commit et configuration. `ContinuousIntegrationBuild` et
`Deterministic` facilitent la traçabilité. Le client desktop affiche sa version ; chaque requête peut
transmettre une version de contrat compatible sans exposer de détail sensible.

Publie symboles et fichiers de diagnostic dans un stockage protégé. Une SBOM inventorie les composants
livrés. Un paquet ou installateur doit être signé quand la plateforme l'exige.

## 3. Deux canaux de livraison {#artefacts}

- **desktop** : installateur signé, mise à jour contrôlée, prérequis, migration de configuration et
  possibilité de revenir à la version précédente ;
- **services** : image OCI non-root et immuable, configuration injectée, déploiement progressif,
  health checks et rollback ;
- **données** : script ou bundle de migration revu, sauvegarde et plan de compatibilité ascendante.

Ne lie pas une mise à jour du poste à un déploiement simultané obligatoire : les contrats doivent
tolérer une fenêtre où plusieurs versions coexistent.

## 4. Promotion {#promotion}

Le même artefact passe intégration, recette puis production. On change la configuration et les
autorisations, pas les binaires. Les preuves de release comprennent résultats de tests, inventaire,
version de schéma, approbation et critères de rollback.

### Atelier réel

Exécute le pipeline sur une MR, télécharge les artefacts, installe le desktop dans une VM propre,
démarre les services via Aspire ou conteneurs, puis simule un rollback de service sans réinstaller le
client.

Référence : [SDK .NET dans les environnements CI](https://learn.microsoft.com/dotnet/devops/dotnet-cli-and-continuous-integration).
