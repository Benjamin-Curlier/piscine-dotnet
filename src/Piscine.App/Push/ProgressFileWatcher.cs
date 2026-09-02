using System.Text.Json;
using Piscine.Core;
using Piscine.Core.Progression;

namespace Piscine.App.Push;

/// <summary>
/// Surveille <c>last-push-result.json</c>, l'artefact exclusivement produit par
/// <c>grade-received</c>. Une écriture de <c>progress.json</c> par un check local, une relecture ou
/// une réinitialisation ne peut donc jamais être présentée comme un résultat de push.
/// </summary>
public sealed class ProgressFileWatcher : IPushResultWatcher
{
    private const int MaxSettleRetries = 8;
    private readonly PiscineLayout _layout;
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new();

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private string? _lastSignature;
    private PushResult? _latest;
    private PushResultDocument? _latestRich;
    private int _settleRetries;
    private bool _started;
    private bool _disposed;

    public ProgressFileWatcher(PiscineLayout layout, TimeProvider? timeProvider = null)
    {
        _layout = layout;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public event Action<PushResult>? ResultReceived;

    public PushResult? LatestResult()
    {
        lock (_lock)
        {
            return _latest;
        }
    }

    public PushResultDocument? LatestRichResult()
    {
        lock (_lock)
        {
            return _latestRich ?? LoadRichSafe();
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_started)
            {
                return;
            }

            _started = true;
        }

        Directory.CreateDirectory(_layout.StateDir);
        var existing = LoadRichSafe();
        lock (_lock)
        {
            _latestRich = existing;
            _lastSignature = existing is null ? null : Signature(existing);
        }

        var watcher = new FileSystemWatcher(_layout.StateDir)
        {
            Filter = Path.GetFileName(_layout.LastPushResultPath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = false,
        };
        watcher.Created += OnChanged;
        watcher.Changed += OnChanged;
        watcher.Renamed += OnRenamed;

        lock (_lock)
        {
            _watcher = watcher;
        }
        watcher.EnableRaisingEvents = true;

        // Ferme la fenêtre entre le snapshot initial et l'activation du FSW : si un push est arrivé
        // exactement pendant Start(), une seconde lecture constate son nouvel identifiant et arme le
        // settle même si l'événement natif a été perdu.
        var afterStart = LoadRichSafe();
        if (afterStart is not null
            && !string.Equals(Signature(afterStart), _lastSignature, StringComparison.Ordinal))
        {
            ArmDebounce();
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => ArmDebounce();

    private void OnRenamed(object sender, RenamedEventArgs e) => ArmDebounce();

    private void ArmDebounce()
    {
        lock (_lock)
        {
            _settleRetries = 0;
            if (_debounceTimer is null)
            {
                _debounceTimer = new Timer(_ => Settle(), null, 250, Timeout.Infinite);
            }
            else
            {
                _debounceTimer.Change(250, Timeout.Infinite);
            }
        }
    }

    private void Settle()
    {
        var document = LoadRichSafe();
        if (document is null)
        {
            RetrySettle();
            return;
        }

        PushResult published;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _settleRetries = 0;
            var signature = Signature(document);
            if (string.Equals(signature, _lastSignature, StringComparison.Ordinal))
            {
                return;
            }

            var entries = document.Exercises.Select(exercise => new PushResultEntry(
                exercise.ExerciseId,
                exercise.Status switch
                {
                    "Reussi" => PushVerdict.Reussi,
                    "EnAttenteRelecture" => PushVerdict.EnAttenteRelecture,
                    _ => PushVerdict.ARevoir,
                },
                exercise.Attempts,
                exercise.LastAttempt)).ToList();

            if (entries.Count == 0)
            {
                _lastSignature = signature;
                _latestRich = document;
                return;
            }

            _latest = new PushResult(
                entries,
                document.GradedAt == default ? _timeProvider.GetLocalNow() : document.GradedAt);
            _latestRich = document;
            _lastSignature = signature;
            published = _latest;
        }

        ResultReceived?.Invoke(published);
    }

    private void RetrySettle()
    {
        lock (_lock)
        {
            if (_disposed || _settleRetries >= MaxSettleRetries)
            {
                _settleRetries = 0;
                return;
            }

            _settleRetries++;
            _debounceTimer?.Change(250, Timeout.Infinite);
        }
    }

    private PushResultDocument? LoadRichSafe()
    {
        try
        {
            return new LastPushResultStore(_layout.LastPushResultPath).Load();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string Signature(PushResultDocument document) =>
        document.PushId ?? JsonSerializer.Serialize(document);

    public ValueTask DisposeAsync()
    {
        FileSystemWatcher? watcher;
        Timer? timer;
        lock (_lock)
        {
            _disposed = true;
            watcher = _watcher;
            timer = _debounceTimer;
            _watcher = null;
            _debounceTimer = null;
        }

        if (watcher is not null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnChanged;
            watcher.Changed -= OnChanged;
            watcher.Renamed -= OnRenamed;
            watcher.Dispose();
        }

        timer?.Dispose();
        return ValueTask.CompletedTask;
    }
}
