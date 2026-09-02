using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Piscine.Core.Model;

namespace Piscine.Core.Progression;

/// <summary>Persiste la progression de la recrue dans un fichier JSON.</summary>
public sealed class ProgressStore
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockRetryDelay = TimeSpan.FromMilliseconds(20);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public ProgressStore(string path)
    {
        _path = path;
    }

    public Progress Load()
    {
        if (!File.Exists(_path))
        {
            return new Progress();
        }

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<Progress>(json, Options) ?? new Progress();
        }
        catch (JsonException)
        {
            // progress.json corrompu (édité à la main, ou écriture précédente interrompue) : on
            // repart d'une progression vide plutôt que de planter `check` et, surtout, le hook
            // `grade-received` (post-receive) qui sinon casserait le `git push` de la recrue.
            return new Progress();
        }
    }

    /// <summary>
    /// Exécute une lecture-modification-écriture sous un verrou inter-processus. Toutes les commandes
    /// qui modifient une progression existante doivent passer par cette méthode afin qu'un
    /// <c>check</c>, un hook <c>grade-received</c> et une réinitialisation concurrents ne puissent pas
    /// sauvegarder chacun un ancien instantané et perdre la mise à jour de l'autre.
    /// </summary>
    public TResult Update<TResult>(Func<Progress, TResult> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        using var transactionLock = AcquireTransactionLock();
        var progress = Load();
        var result = update(progress);
        Save(progress);
        return result;
    }

    /// <summary>Variante sans valeur de retour de <see cref="Update{TResult}"/>.</summary>
    public void Update(Action<Progress> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        Update(progress =>
        {
            update(progress);
            return true;
        });
    }

    public void Save(Progress progress)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(progress, Options);

        // Écriture atomique : on écrit un fichier temporaire voisin puis on le déplace par-dessus la
        // cible, pour ne jamais laisser un progress.json à moitié écrit (que Load() devrait ensuite
        // récupérer) si le process est interrompu en plein File.WriteAllText. Le nom du temporaire est
        // UNIQUE par écriture (GUID) : le hook grade-received (post-receive) et un `check` CLI/Desktop
        // peuvent sauvegarder en parallèle sans se disputer un même « .tmp » (IOException de partage,
        // ou File.Move sur un temp déjà consommé par l'autre process).
        var temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, json);
            File.Move(temp, _path, overwrite: true);
        }
        finally
        {
            TryDeleteTemp(temp);
        }
    }

    private FileStream AcquireTransactionLock()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Le fichier reste en place : le supprimer à la libération introduirait une course où un
        // troisième processus pourrait créer un nouvel inode tandis qu'un second attend l'ancien.
        var lockPath = _path + ".lock";
        var deadline = DateTime.UtcNow + LockTimeout;
        while (true)
        {
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.None);
            }
            catch (IOException) when (DateTime.UtcNow < deadline)
            {
                Thread.Sleep(LockRetryDelay);
            }
        }
    }

    /// <summary>Supprime le temporaire s'il subsiste (échec avant/pendant le Move). Best-effort.</summary>
    private static void TryDeleteTemp(string temp)
    {
        try
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // On ne laisse jamais l'échec du nettoyage masquer la vraie erreur (ni casser le Move réussi).
        }
    }
}
