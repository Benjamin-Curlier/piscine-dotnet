using Microsoft.Extensions.Logging;

// FOURNI — ne modifie pas ce fichier.
// Un fournisseur de logs minimal qui écrit chaque entrée sur la sortie standard,
// de façon SYNCHRONE et déterministe, au format : "Catégorie [Niveau] message".
// (Le provider console par défaut écrit en arrière-plan : sortie non déterministe.)
sealed class CaptureLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName);

    public void Dispose() { }
}

sealed class CaptureLogger : ILogger
{
    private readonly string _category;

    public CaptureLogger(string category) => _category = category;

    public System.IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        System.Exception? exception,
        System.Func<TState, System.Exception?, string> formatter)
    {
        System.Console.WriteLine($"{_category} [{logLevel}] {formatter(state, exception)}");
    }

    private sealed class NullScope : System.IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}
