namespace Piscine.Core;

/// <summary>
/// Construit le texte de la bannière d'accueil affichée par la CLI.
/// </summary>
public static class WelcomeBanner
{
    public static string Render(string version)
    {
        return $"""
            ┌──────────────────────────────┐
            │        Piscine .NET          │
            │      bootcamp C# / git       │
            └──────────────────────────────┘
            version {version}
            """;
    }
}
