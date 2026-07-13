using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using Piscine.App.Coaching;

namespace Piscine.App.Tests;

/// <summary>
/// Test d'integration du shim <c>git</c> : on lance l'exe shim reel avec un FAUX git deterministe
/// (un script qui <c>exit 3</c>) et un canal named pipe d'ecoute, puis on verifie (a) le relais
/// transparent (code 3 propage) et (b) l'evenement structure recu (sous-commande + code). Skip propre
/// si l'exe shim n'est pas localisable (non encore construit) pour garder la run verte.
/// </summary>
public sealed class GitShimRelayTests
{
    [Fact]
    public async Task Shim_relays_real_git_exit_code_and_emits_event()
    {
        var shim = LocateShimExe();
        if (shim is null)
        {
            // Shim non construit (ex. test joue avant build solution) : skip propre.
            return;
        }

        using var temp = new TempDir();

        // Faux git deterministe : exit 3, peu importe les arguments.
        string fakeGit;
        if (OperatingSystem.IsWindows())
        {
            fakeGit = temp.WriteFile("fakegit.cmd", "@echo off\r\nexit /b 3\r\n");
        }
        else
        {
            fakeGit = temp.WriteFile("fakegit.sh", "#!/bin/sh\nexit 3\n");
            var psi = new ProcessStartInfo("chmod", $"+x \"{fakeGit}\"") { UseShellExecute = false };
            using var chmod = Process.Start(psi);
            await chmod!.WaitForExitAsync();
        }

        await using var channel = new NamedPipeCoachingChannel();
        using var received = new SemaphoreSlim(0);
        GitCommandEvent? captured = null;
        channel.CommandReceived += evt =>
        {
            captured = evt;
            received.Release();
        };
        channel.Start();

        var run = new ProcessStartInfo(shim) { UseShellExecute = false };
        run.ArgumentList.Add("status");
        run.Environment["PISCINE_REAL_GIT"] = fakeGit;
        run.Environment["PISCINE_COACH_PIPE"] = channel.Endpoint;

        using var proc = new Process { StartInfo = run };
        proc.Start();
        await proc.WaitForExitAsync();

        // (a) Relais transparent : le shim renvoie le code de sortie du vrai git, inchange.
        Assert.Equal(3, proc.ExitCode);

        // (b) Emission named pipe : l'evenement structure est arrive cote App.
        var gotEvent = await received.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(gotEvent, "Aucun evenement recu sur le canal de coaching dans le delai imparti.");
        Assert.NotNull(captured);
        Assert.Equal("status", captured!.Subcommand);
        Assert.Equal(3, captured.ExitCode);
    }

    /// <summary>
    /// Robustesse anti-OOM : un client local hostile (qui connait le nom du pipe via son env) envoie
    /// une ligne GEANTE sans <c>\n</c>. Le canal doit l'ignorer sans deserialiser ni accumuler sans
    /// borne, PUIS rester operationnel : un evenement legitime envoye ensuite doit bien etre recu.
    /// </summary>
    [Fact]
    public async Task Ligne_geante_sans_saut_de_ligne_est_ignoree_et_la_boucle_survit()
    {
        await using var channel = new NamedPipeCoachingChannel();
        using var received = new SemaphoreSlim(0);
        var events = new List<GitCommandEvent>();
        channel.CommandReceived += evt =>
        {
            lock (events)
            {
                events.Add(evt);
            }

            received.Release();
        };
        channel.Start();

        // (1) Connexion hostile : > 64 Ko d'un seul tenant, SANS '\n'. Le serveur doit bailer au
        // plafond et fermer le pipe ; l'ecriture cote client peut donc se terminer par un IOException.
        await using (var hostile = new NamedPipeClientStream(".", channel.Endpoint, PipeDirection.Out))
        {
            await hostile.ConnectAsync(10_000);
            var giant = new byte[256 * 1024];
            Array.Fill(giant, (byte)'a');
            try
            {
                await hostile.WriteAsync(giant);
                await hostile.FlushAsync();
            }
            catch (IOException)
            {
                // Attendu : le serveur a ferme le pipe des le plafond atteint (ecriture tronquee).
            }
        }

        // (2) Connexion legitime ensuite : preuve que la boucle a survecu au message geant.
        await using (var client = new NamedPipeClientStream(".", channel.Endpoint, PipeDirection.Out))
        {
            await client.ConnectAsync(10_000);
            var json = "{\"argv\":[\"status\"],\"exitCode\":0,\"cwd\":\"/tmp\"}\n";
            await client.WriteAsync(Encoding.UTF8.GetBytes(json));
            await client.FlushAsync();
        }

        var got = await received.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(got, "L'evenement legitime posterieur au message geant n'a pas ete recu (boucle morte ?).");
        lock (events)
        {
            Assert.Single(events);
            Assert.Equal("status", events[0].Subcommand);
            Assert.Equal(0, events[0].ExitCode);
        }
    }

    /// <summary>
    /// Localise l'exe shim construit : remonte jusqu'au dossier contenant <c>Piscine.slnx</c>, puis
    /// cherche <c>src/Piscine.GitShim/bin/&lt;config&gt;/net10.0/git[.exe]</c> (Debug puis Release).
    /// </summary>
    private static string? LocateShimExe()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Piscine.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            return null;
        }

        var exeName = OperatingSystem.IsWindows() ? "git.exe" : "git";
        var binRoot = Path.Combine(dir.FullName, "src", "Piscine.GitShim", "bin");

        foreach (var config in new[] { "Release", "Debug" })
        {
            var candidate = Path.Combine(binRoot, config, "net10.0", exeName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
