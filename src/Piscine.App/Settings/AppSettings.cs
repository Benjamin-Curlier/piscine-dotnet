namespace Piscine.App.Settings;

/// <summary>Réglages de l'app (minimal S2 : surcharge de la commande éditeur). Étendu en S6.</summary>
public sealed record AppSettings
{
    /// <summary>Commande éditeur surchargée (ex. « code », « rider »). null = auto-détection.</summary>
    public string? EditorCommand { get; init; }
}
