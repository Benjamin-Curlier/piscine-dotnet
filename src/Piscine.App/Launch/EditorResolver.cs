using System;

namespace Piscine.App.Launch;

/// <summary>Un éditeur lançable : libellé affiché + commande.</summary>
public sealed record EditorOption(string Label, string FileName);

/// <summary>
/// Choix de l'éditeur : la surcharge (Réglages) prime ; sinon 1ʳᵉ commande candidate présente dans le
/// PATH (sonde injectée → testable). null si rien (l'UI retombe sur « ouvrir le dossier »). Pur.
/// </summary>
public static class EditorResolver
{
    private static readonly (string Label, string Cmd)[] Candidates =
        [("VS Code", "code"), ("Rider", "rider"), ("Visual Studio", "devenv")];

    public static EditorOption? Resolve(string? overrideCommand, Func<string, bool> isOnPath)
    {
        if (!string.IsNullOrWhiteSpace(overrideCommand))
        {
            return new EditorOption(overrideCommand!, overrideCommand!);
        }

        foreach (var (label, cmd) in Candidates)
        {
            if (isOnPath(cmd))
            {
                return new EditorOption(label, cmd);
            }
        }

        return null;
    }
}
