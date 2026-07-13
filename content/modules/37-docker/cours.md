# Module 37 — Docker & conteneurisation (.NET 10)

> **Module de lecture.** Pas d'exercices auto-corrigés : construire et lancer une image dépend de
> l'installation de Docker sur **ta** machine (et de l'accès réseau à des registres), ce qui n'est ni
> déterministe ni portable pour la moulinette. Lis ce module, puis **expérimente en local** avec les
> commandes ci-dessous.

Jusqu'ici tu livrais un exécutable. Mais « ça marche sur ma machine » ne suffit pas : il faut la
**même version du runtime**, les **mêmes dépendances**, la **même configuration** partout. Un
**conteneur** empaquette ton application **et** son environnement dans une unité isolée et
reproductible, qui tourne à l'identique sur ton poste, en CI et en production.

---

## 1. Image, conteneur, registre {#vocabulaire}

- **Image** : un modèle **immuable** en lecture seule (ton appli + le runtime + les fichiers
  nécessaires), construit en **couches** empilées et mises en cache.
- **Conteneur** : une **instance en cours d'exécution** d'une image (un processus isolé du reste du
  système : système de fichiers, réseau, etc.).
- **Registre** : un dépôt d'images (Docker Hub, GitHub Container Registry `ghcr.io`, Azure
  Container Registry…). On **push** une image, on la **pull** ailleurs.

Analogie : l'image est à la **classe** ce que le conteneur est à l'**instance**.

---

## 2. Le Dockerfile multi-étapes (l'approche classique) {#dockerfile}

Un `Dockerfile` décrit comment construire l'image. Pour .NET, on utilise un **build multi-étapes** :
une étape « SDK » lourde pour compiler, une étape « runtime » légère pour exécuter — l'image finale
ne contient **que** le runtime et la publication, pas le SDK.

```dockerfile
# Étape 1 : build (image SDK, lourde)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# Étape 2 : runtime (image légère, sans SDK)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MonAppli.dll"]
```

> ⚠️ `COPY *.csproj .` ne marche que pour un **projet unique** à la racine du contexte de build.
> Dans une **solution multi-projets**, ce glob **aplatit** tous les `.csproj` dans un seul dossier
> et casse `dotnet restore` (les références entre projets, qui sont des chemins relatifs, ne
> pointent plus nulle part). Copie alors le `.sln` et chaque `.csproj` **en conservant son
> arborescence** (`COPY src/MonAppli/MonAppli.csproj src/MonAppli/`, une ligne par projet) avant de
> faire `restore`, ou copie tout le source d'un coup (au prix du cache de couches).

Images de base Microsoft (`mcr.microsoft.com/dotnet/…`) :

- **`sdk`** : compiler/publier (build uniquement) ;
- **`aspnet`** : applications web ASP.NET Core ;
- **`runtime`** : applications console/services qui n'ont pas besoin d'ASP.NET ;
- **`runtime-deps`** : pour le déploiement **self-contained** (sans runtime .NET installé).

Construire et lancer :

```bash
docker build -t monappli:1.0 .
docker run --rm -p 8080:8080 monappli:1.0
```

`-p 8080:8080` mappe un port de l'hôte vers le conteneur ; `--rm` supprime le conteneur à l'arrêt.

---

## 3. Publier une image SANS Dockerfile (SDK .NET 10) {#sdk-publish}

Le SDK .NET sait construire une image OCI **directement**, sans écrire de `Dockerfile`. C'est souvent
la voie la plus simple pour une application .NET :

```bash
dotnet publish -c Release -t:PublishContainer
```

On personnalise l'image via des **propriétés MSBuild** (dans le `.csproj` ou en ligne de commande) :

```xml
<PropertyGroup>
  <ContainerRepository>monappli</ContainerRepository>
  <ContainerImageTags>1.0;latest</ContainerImageTags>
  <!-- Image de base « chiseled » : minimale, sans shell, exécutée en non-root -->
  <ContainerFamily>noble-chiseled</ContainerFamily>
</PropertyGroup>
```

Pour cibler une plateforme précise et pousser vers un registre :

```bash
dotnet publish -c Release -t:PublishContainer \
  --os linux --arch x64 \
  -p:ContainerRegistry=ghcr.io
```

Avantages : pas de `Dockerfile` à maintenir, build reproductible piloté par le SDK, intégration
naturelle dans la CI.

---

## 4. Des images petites et sûres {#images-minimales}

Une image plus petite = build et déploiement plus rapides, et **surface d'attaque réduite**.

- **Images `chiseled`** (Ubuntu *chiseled* / distroless) : ne contiennent que le strict nécessaire —
  **pas de shell**, pas de gestionnaire de paquets — et tournent en **non-root** par défaut. Idéal
  en production.
- **`.dockerignore`** : exclure `bin/`, `obj/`, `.git/`… du contexte de build (build plus rapide,
  pas de fuite de fichiers).
- **Ordre des couches** : copier le `.csproj` et faire `restore` **avant** de copier tout le code,
  pour que le cache des dépendances ne soit pas invalidé à chaque changement de source.
- **AOT / trimming** : pour des images encore plus petites, voir la compilation Native AOT (hors
  périmètre ici).

---

## 5. Configuration & exécution {#configuration}

- **Variables d'environnement** : la config .NET lit les variables d'environnement ; en conteneur
  c'est le moyen standard d'injecter des réglages (`-e "ASPNETCORE_URLS=http://+:8080"`).
- **Ne jamais** mettre de secret en dur dans une image (elle est distribuée et inspectable) :
  injecter par variable d'env ou un gestionnaire de secrets.
- **Ports** : l'appli écoute *dans* le conteneur ; `-p hôte:conteneur` l'expose vers l'extérieur.
- Commandes utiles : `docker images`, `docker ps`, `docker logs <id>`, `docker stop <id>`,
  `docker rmi <image>`.

---

## 6. À pratiquer (sur ta machine) {#pratique}

1. **Installe Docker Desktop** (ou le moteur Docker sous Linux) et vérifie : `docker run hello-world`.
2. Crée une petite appli console ou web .NET 10, puis **conteneurise-la des deux façons** :
   un `Dockerfile` multi-étapes, **puis** `dotnet publish -t:PublishContainer`.
3. Compare la **taille** des images (`docker images`) entre une base `aspnet` et une base
   `chiseled` (`ContainerFamily=noble-chiseled`).
4. Lance le conteneur, mappe un port, consulte les logs (`docker logs`), arrête-le.
5. (Optionnel) Pousse l'image vers **GitHub Container Registry** (`ghcr.io`) et pull-la ailleurs.

## Références externes

- Microsoft Learn — *Containerize a .NET app* et *Container images for .NET apps*.
- Documentation SDK — *Containerize an app with `dotnet publish`* (`PublishContainer`).
- *.NET chiseled Ubuntu images* (blog .NET).
- Documentation officielle Docker — *Build*, *Dockerfile reference*.
