using System;

namespace Piscine.App.Board;

/// <summary>Avancement d'un module pour le tableau de bord : exercices « faits » sur le total.</summary>
public sealed record ModuleProgress(string Number, string Title, int Done, int Total)
{
    public int Percent => Total == 0 ? 0 : (int)Math.Round(100.0 * Done / Total);
}
