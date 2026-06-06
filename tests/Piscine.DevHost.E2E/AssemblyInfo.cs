using Xunit;

// Chaque test E2E démarre son PROPRE DevHost via `dotnet run` (qui déclenche un build MSBuild de
// Piscine.DevHost). Lancés en parallèle, plusieurs `dotnet run` simultanés se disputent la même
// sortie bin/ et les verrous de build → démarrages qui expirent. On sérialise donc les collections
// de ce projet : chaque serveur démarre seul, sans contention.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
