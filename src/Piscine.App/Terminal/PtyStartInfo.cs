namespace Piscine.App.Terminal;

/// <summary>Parametres de demarrage d'une session PTY. Le shell par defaut depend de l'OS.</summary>
public sealed record PtyStartInfo
{
    /// <summary>Executable du shell. Par defaut : cmd (Windows), /bin/bash (Unix).</summary>
    public string? Shell { get; init; }
    public string WorkingDirectory { get; init; } = Environment.CurrentDirectory;
    public int Cols { get; init; } = 80;
    public int Rows { get; init; } = 24;

    /// <summary>Shell par defaut selon l'OS hote (deterministe, testable).</summary>
    public static string DefaultShell() =>
        OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe")
            : "/bin/bash";
}
