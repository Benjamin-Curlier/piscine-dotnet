# ex00 — Dockerfile multi-étapes

Écris le `Dockerfile` d'un projet unique `Asteria.Api.csproj` :

1. stage `build` depuis `mcr.microsoft.com/dotnet/sdk:10.0` ;
2. `WORKDIR /src`, copie du csproj puis `dotnet restore` pour préserver le cache ;
3. copie du reste et `dotnet publish -c Release -o /app/publish --no-restore` ;
4. stage final `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` ;
5. copie de la publication, `USER $APP_UID`, puis entrée `Asteria.Api.dll`.

N'utilise ni tag `latest`, ni installation `apt-get`, ni secret `PASSWORD=`. La moulinette inspecte
le fichier sans construire l'image.
