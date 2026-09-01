# Module 44 — Socle .NET d'entreprise

Une application lourde distribuée n'est jamais un unique fichier C#. Elle devient une **solution**
composée de projets, de dépendances, de règles de compilation et de configurations différentes selon
le poste, le banc de test et la production.

## 1. La solution et les projets {#projets}

La solution (`.slnx`) regroupe les projets. Chaque `.csproj` définit son SDK, sa cible, ses références
de projets et ses paquets. Une frontière de projet est une frontière de compilation : si `Domain` ne
référence pas `Infrastructure`, le compilateur empêche une dépendance interdite.

Une organisation fréquente pour un produit desktop distribué est :

- `Asteria.Domain` : règles métier et contrats sans infrastructure ;
- `Asteria.Application` : cas d'usage, dépend seulement du domaine ;
- `Asteria.Infrastructure` : stockage, réseau, broker et adaptateurs ;
- `Asteria.Desktop` : interface et composition root ;
- des projets de tests séparés selon leur portée.

`ProjectReference` exprime ces liens. Évite de contourner les frontières en copiant des types ou en
ajoutant une référence « juste pour compiler ».

## 2. Une politique de build partagée {#build-props}

`Directory.Build.props` est importé automatiquement par les projets situés sous son dossier. Il peut
centraliser `TargetFramework`, nullable, analyseurs, style et avertissements bloquants. Les projets
gardent seulement leurs différences réelles.

La CI doit reconstruire avec le même SDK épinglé dans `global.json`. Un build reproductible limite
les écarts « fonctionne sur mon poste ».

## 3. Configuration et options {#options}

Le Generic Host agrège les sources dans un ordre explicite : fichiers `appsettings`, secrets de
développement, variables d'environnement puis arguments. Une valeur sensible ne va ni dans Git ni
dans une image.

Le pattern Options transforme une section en type métier. Une application de production valide ses
options au démarrage (`ValidateOnStart`) afin d'échouer avant d'accepter du trafic ou d'ouvrir une
session opérateur. `IValidateOptions<T>` couvre les règles qui dépassent les attributs simples.

## 4. Contrat de livraison {#contrat-build}

Le dépôt doit pouvoir exécuter, sans IDE : restore, build, tests, analyse, publication et création
d'artefact. Les mêmes commandes servent au développeur et à la CI. Les paquets privés, versions et
sources NuGet sont déclarés, jamais devinés depuis une machine particulière.

### Atelier réel

Crée une solution avec les quatre projets Asteria, active les règles partagées, introduis un warning
nullable et constate qu'il bloque le build. Ajoute ensuite une configuration invalide et vérifie que
l'hôte refuse de démarrer avec un message exploitable.

Références :

- [Propriétés MSBuild du SDK .NET](https://learn.microsoft.com/dotnet/core/project-sdk/msbuild-props)
- [Personnaliser le build avec Directory.Build.props](https://learn.microsoft.com/visualstudio/msbuild/customize-by-directory)
- [Options dans .NET](https://learn.microsoft.com/dotnet/core/extensions/options)
