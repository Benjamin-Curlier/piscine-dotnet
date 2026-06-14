using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Piscine.App.Checking;
using Piscine.App.Coaching;
using Piscine.App.Git;
using Piscine.App.Init;
using Piscine.App.Launch;
using Piscine.App.Progress;
using Piscine.App.Push;
using Piscine.App.Settings;
using Piscine.App.Terminal;
using Piscine.Components;
using Piscine.Components.Services;
using Piscine.Core;
using Piscine.Grading;

namespace Piscine.Desktop;

internal static class Program
{
    // [STAThread] OBLIGATOIRE : WebView2 (Blazor Hybrid) repose sur COM et exige un thread principal
    // en appartement mono-thread (STA). Des top-level statements produisent un Main MTA → la fenêtre
    // native s'ouvre mais la webview NE REND RIEN (écran noir). Un Main explicite annoté [STAThread]
    // corrige le rendu. Cf. docs/superpowers/retex/2026-06-14-desktop-blank-screen.md.
    [STAThread]
    private static void Main(string[] args)
    {
        // Hôte BlazorWebView (Photino) : annuler les render modes (les pages RCL qui portent
        // @rendermode InteractiveServer deviennent du rendu in-process, interactif par nature).
        // Doit précéder toute construction/rendu de composant.
        InteractiveRenderSettings.ConfigureBlazorHybridRenderModes();

        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        // PhotinoBlazorAppBuilder n'enregistre PAS d'IConfiguration (contrairement à WebApplication du
        // DevHost) ; CourseCatalog en dépend (ContentRootResolver lit PISCINE_CONTENT). On en fournit une
        // basée sur les variables d'environnement — sinon le rendu jette « Unable to resolve IConfiguration ».
        builder.Services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder().AddEnvironmentVariables().Build());

        // Contenu pédagogique (chargé une fois) + rendu Markdown, partagés par toutes les pages.
        builder.Services.AddSingleton<CourseCatalog>();
        builder.Services.AddSingleton<MarkdownRenderer>();

        // Layout piscine depuis l'environnement (résolveur partagé PiscineLayout.FromEnvironment, identique au
        // CLI/hook) : contenu = PISCINE_CONTENT (sinon racine du catalogue embarqué), workspace = PISCINE_WORKSPACE
        // (sinon home/workspace), état sous PISCINE_HOME.
        builder.Services.AddSingleton(sp =>
        {
            var catalog = sp.GetRequiredService<CourseCatalog>();
            return PiscineLayout.FromEnvironment(defaultContentRoot: catalog.ContentRoot);
        });

        // Statut git (LibGit2Sharp, lecture seule) — requis par ProgressService.
        builder.Services.AddSingleton<GitStatusService>();

        // Vérification in-process (rejoue la chaîne du moteur, sans console ni progression).
        builder.Services.AddSingleton(sp =>
            new CheckService(sp.GetRequiredService<PiscineLayout>(), Graders.Default()));

        // Progression par exercice (lecture seule : progress.json + état git + livrables présents).
        builder.Services.AddSingleton(sp => new ProgressService(
            sp.GetRequiredService<PiscineLayout>(),
            sp.GetRequiredService<GitStatusService>()));

        // Initialisation du workspace (enrobe GitWorkspace.Initialize) ; chemin de l'exe surclassable
        // par PISCINE_EXE (le hook post-receive doit pointer le binaire piscine du zip).
        builder.Services.AddSingleton(sp =>
        {
            var layout = sp.GetRequiredService<PiscineLayout>();
            var exe = Environment.GetEnvironmentVariable("PISCINE_EXE") ?? "piscine";
            return new InitService(layout, exe);
        });

        // Surveillant du résultat de push (observe progress.json écrit par grade-received).
        builder.Services.AddSingleton<IPushResultWatcher>(sp =>
            new ProgressFileWatcher(sp.GetRequiredService<PiscineLayout>()));

        // Terminal embarqué + coaching git (S12) : shell OS LOCAL in-process (Photino) — pas de réseau.
        // PtyService lance le shell ; le shim git (packagé dans desktop/gitshim/, résolu par ShimLocator)
        // émet ses commandes sur un canal named-pipe ; CoachingService en dérive les cartes.
        // TerminalPolicy(true) : activé (app de bureau locale, contrairement au harnais DevHost).
        builder.Services.AddSingleton<PtyService>();
        builder.Services.AddSingleton<CoachingService>();
        builder.Services.AddSingleton<ICoachingChannel, NamedPipeCoachingChannel>();
        builder.Services.AddSingleton(new TerminalPolicy(enabled: true));

        // « Ouvrir » l'exercice (S2) : dossier / éditeur / terminal système via un launcher réel.
        builder.Services.AddSingleton<IProcessLauncher, ProcessLauncher>();
        builder.Services.AddSingleton<WorkspaceLauncher>();
        builder.Services.AddSingleton<SettingsService>();

        builder.RootComponents.Add<App>("#app");

        var app = builder.Build();

        app.MainWindow
           .SetTitle("Piscine .NET")
           .SetUseOsDefaultSize(true);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            app.MainWindow.ShowMessage("Erreur fatale", e.ExceptionObject.ToString() ?? "Exception inconnue");

        // Mode smoke headless (PISCINE_SMOKE=1) : capture le bilan de rendu de la fenêtre puis termine.
        var smokeOut = Environment.GetEnvironmentVariable("PISCINE_SMOKE_OUT");
        if (Environment.GetEnvironmentVariable("PISCINE_SMOKE") == "1" && !string.IsNullOrEmpty(smokeOut))
        {
            SmokeProbe.Attach(app, smokeOut);
        }

        app.Run();
    }
}
