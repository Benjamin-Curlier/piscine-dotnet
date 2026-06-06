namespace Piscine.App.Terminal;

/// <summary>Une session PTY vivante : on lit sa sortie, on lui ecrit, on la redimensionne.</summary>
public interface IPtySession : IAsyncDisposable
{
    /// <summary>Octets bruts emis par le shell (deja decodes du flux PTY).</summary>
    event Action<byte[]>? Output;

    /// <summary>Declenche quand le processus shell se termine (transporte le code de sortie).</summary>
    event Action<int>? Exited;

    Task WriteAsync(string data, CancellationToken ct = default);
    void Resize(int cols, int rows);
}
