namespace Piscine.App.Terminal;

/// <summary>Parametres de demarrage d'une session PTY. Le shell par defaut depend de l'OS.</summary>
public sealed record PtyStartInfo
{
    /// <summary>Executable du shell. Par defaut : cmd (Windows), /bin/bash (Unix).</summary>
    public string? Shell { get; init; }
    public string WorkingDirectory { get; init; } = System.Environment.CurrentDirectory;
    public int Cols { get; init; } = 80;
    public int Rows { get; init; } = 24;

    /// <summary>
    /// Variables d'environnement additionnelles / surcharges fusionnees dans l'env de la session
    /// (ex. PATH prefixe par le dossier du shim, <c>PISCINE_REAL_GIT</c>, <c>PISCINE_COACH_PIPE</c>).
    /// <c>null</c> = aucune surcharge (comportement S2 inchange).
    /// </summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    /// <summary>Shell par defaut selon l'OS hote (deterministe, testable).</summary>
    public static string DefaultShell() =>
        OperatingSystem.IsWindows()
            ? (System.Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe")
            : "/bin/bash";
}
