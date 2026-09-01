namespace Piscine.App.Terminal;

/// <summary>Une session PTY vivante : on lit sa sortie, on lui ecrit, on la redimensionne.</summary>
public interface IPtySession : IAsyncDisposable
{
    /// <summary>Octets bruts emis par le shell (deja decodes du flux PTY).</summary>
    public event Action<byte[]>? Output;

    /// <summary>Declenche quand le processus shell se termine (transporte le code de sortie).</summary>
    public event Action<int>? Exited;

    public Task WriteAsync(string data, CancellationToken ct = default);
    public void Resize(int cols, int rows);
}
