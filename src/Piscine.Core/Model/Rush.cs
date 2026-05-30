namespace Piscine.Core.Model;

/// <summary>
/// Un Rush : projet de synthèse solo, autonome (un seul livrable noté), découvert
/// sous <c>content/rushes/</c>. Réutilise le manifest et les graders des exercices.
/// </summary>
public sealed record Rush(string Id, string Title, string ContentDir);
