namespace Piscine.App.Settings;

/// <summary>Thème de l'app : suit le système, ou forcé clair/sombre (persisté en S6).</summary>
public enum AppTheme
{
    /// <summary>Suit la préférence du système d'exploitation (<c>prefers-color-scheme</c>).</summary>
    System,

    /// <summary>Toujours clair.</summary>
    Light,

    /// <summary>Toujours sombre.</summary>
    Dark,
}

/// <summary>Cible par défaut du bouton « Ouvrir » → terminal (S6).</summary>
public enum TerminalTarget
{
    /// <summary>Terminal intégré à l'app (page <c>/terminal</c>, coaching git).</summary>
    Embedded,

    /// <summary>Terminal système de l'OS, ouvert dans le dossier de l'exercice.</summary>
    System,
}

/// <summary>
/// Réglages de l'app. Minimal en S2 (surcharge de la commande éditeur) ; étendu en S6 avec le thème,
/// l'échelle de police et la cible terminal par défaut, tous persistés via <see cref="SettingsService"/>.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Échelle de police minimale (50&nbsp;%).</summary>
    public const double MinFontScale = 0.8;

    /// <summary>Échelle de police maximale (150&nbsp;%).</summary>
    public const double MaxFontScale = 1.5;

    /// <summary>Échelle de police par défaut (100&nbsp;%).</summary>
    public const double DefaultFontScale = 1.0;

    /// <summary>Commande éditeur surchargée (ex. « code », « rider »). null = auto-détection.</summary>
    public string? EditorCommand { get; init; }

    /// <summary>Thème de l'interface (par défaut : suivre le système).</summary>
    public AppTheme Theme { get; init; } = AppTheme.System;

    /// <summary>
    /// Échelle de police pour la lisibilité, bornée à [<see cref="MinFontScale"/>,
    /// <see cref="MaxFontScale"/>] par <see cref="Normalized"/>. 1.0 = taille standard.
    /// </summary>
    public double FontScale { get; init; } = DefaultFontScale;

    /// <summary>Cible par défaut du terminal au clic « Ouvrir » (par défaut : intégré).</summary>
    public TerminalTarget DefaultTerminal { get; init; } = TerminalTarget.Embedded;

    /// <summary>
    /// Renvoie une copie aux valeurs assainies : <see cref="FontScale"/> borné dans
    /// [<see cref="MinFontScale"/>, <see cref="MaxFontScale"/>] (et remplacé par
    /// <see cref="DefaultFontScale"/> s'il est NaN/non fini). Sûr à appeler après lecture du JSON,
    /// dont les valeurs peuvent avoir été éditées à la main.
    /// </summary>
    public AppSettings Normalized() => this with { FontScale = ClampFontScale(FontScale) };

    /// <summary>Borne une échelle de police dans la plage autorisée (NaN/∞ → défaut).</summary>
    public static double ClampFontScale(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return DefaultFontScale;
        }

        return System.Math.Clamp(value, MinFontScale, MaxFontScale);
    }
}
