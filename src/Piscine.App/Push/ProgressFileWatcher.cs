using System.Text.Json;
using Piscine.Core;
using Piscine.Core.Progression;
using CoreProgress = Piscine.Core.Model.Progress;
using ExerciseProgress = Piscine.Core.Model.ExerciseProgress;
using ExerciseStatus = Piscine.Core.Model.ExerciseStatus;

namespace Piscine.App.Push;

/// <summary>
/// Impl de <see cref="IPushResultWatcher"/> basée sur <see cref="FileSystemWatcher"/>.
/// Surveille <c>progress.json</c> dans <c>StateDir</c>, relit via <see cref="ProgressStore"/>
/// à chaque settle (debounce 250 ms) et publie uniquement les delta réels.
/// <b>Lecture seule</b> : n'appelle jamais <c>ProgressStore.Save</c>.
/// </summary>
public sealed class ProgressFileWatcher : IPushResultWatcher
{
    private readonly PiscineLayout _layout;

    // Protège _latest et _last contre les accès concurrents (thread FSW vs thread UI).
    private readonly object _lock = new();

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private CoreProgress _last = new();
    private PushResult? _latest;
    private bool _started;

    public ProgressFileWatcher(PiscineLayout layout)
    {
        _layout = layout;
    }

    /// <inheritdoc/>
    public event Action<PushResult>? ResultReceived;

    /// <inheritdoc/>
    public PushResult? LatestResult()
    {
        lock (_lock)
        {
            return _latest;
        }
    }

    /// <inheritdoc/>
    public void Start()
    {
        lock (_lock)
        {
            if (_started) return;
            _started = true;
        }

        // Créer le dossier si nécessaire (FSW lève si inexistant).
        Directory.CreateDirectory(_layout.StateDir);

        // Snapshot initial — absorbe l'état existant sans publier.
        lock (_lock)
        {
            _last = LoadSafe() ?? new CoreProgress();
        }

        // Créer le watcher sur le dossier (pas le fichier directement).
        var watcher = new FileSystemWatcher(_layout.StateDir)
        {
            Filter = "progress.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        watcher.Created += OnChanged;
        watcher.Changed += OnChanged;
        watcher.Renamed += OnRenamed;

        lock (_lock)
        {
            _watcher = watcher;
        }
    }

    // ── Handlers FSW ──────────────────────────────────────────────────────────

    private void OnChanged(object sender, FileSystemEventArgs e) => ArmDebounce();

    private void OnRenamed(object sender, RenamedEventArgs e) => ArmDebounce();

    private void ArmDebounce()
    {
        lock (_lock)
        {
            // (Re)arme le timer à 250 ms ; chaque nouvel événement reporte l'échéance.
            if (_debounceTimer is null)
            {
                _debounceTimer = new Timer(
                    _ => Settle(),
                    state: null,
                    dueTime: 250,
                    period: Timeout.Infinite);
            }
            else
            {
                _debounceTimer.Change(250, Timeout.Infinite);
            }
        }
    }

    private void Settle()
    {
        CoreProgress current;
        try
        {
            current = LoadSafe() ?? new CoreProgress();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            // Lecture interrompue (écriture en cours) → réarmer une fois.
            lock (_lock)
            {
                _debounceTimer?.Change(250, Timeout.Infinite);
            }
            return;
        }

        List<PushResultEntry> delta;
        lock (_lock)
        {
            delta = ComputeDelta(_last, current);
            if (delta.Count == 0) return;

            _latest = new PushResult(delta, DateTimeOffset.Now);
            _last = current;
        }

        ResultReceived?.Invoke(_latest!);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private CoreProgress? LoadSafe()
    {
        try
        {
            return new ProgressStore(_layout.ProgressPath).Load();
        }
        catch (FileNotFoundException)
        {
            return new CoreProgress();
        }
    }

    private static List<PushResultEntry> ComputeDelta(CoreProgress last, CoreProgress current)
    {
        var delta = new List<PushResultEntry>();

        foreach (var (id, ep) in current.Exercises)
        {
            bool changed = !last.Exercises.TryGetValue(id, out var prev)
                || prev.Status != ep.Status
                || prev.Attempts != ep.Attempts;

            if (changed)
            {
                delta.Add(new PushResultEntry(
                    ExerciseId: id,
                    Verdict: ep.Status == ExerciseStatus.Reussi ? PushVerdict.Reussi : PushVerdict.ARevoir,
                    Attempts: ep.Attempts,
                    LastAttempt: ep.LastAttempt));
            }
        }

        return delta;
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    public ValueTask DisposeAsync()
    {
        FileSystemWatcher? watcher;
        Timer? timer;

        lock (_lock)
        {
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
