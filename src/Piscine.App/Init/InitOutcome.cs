namespace Piscine.App.Init;

/// <summary>Résultat d'un appel à <see cref="InitService.Initialize"/>.</summary>
public record InitOutcome(
    bool Success,
    InitStatus Before,
    InitStatus After,
    string Message,
    string? Error);
