using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Piscine.App.Checking;
using Piscine.App.Coaching;
using Piscine.App.Git;
using Piscine.App.Init;
using Piscine.App.Progress;
using Piscine.App.Push;
using Piscine.App.Terminal;
using Piscine.Components;
using Piscine.Components.Services;
using Piscine.Core;
using Piscine.Grading;

// Hôte BlazorWebView (Photino) : annuler les render modes (les pages RCL qui portent
// @rendermode InteractiveServer deviennent du rendu in-process, interactif par nature).
// Doit précéder toute construction/rendu de composant.
InteractiveRenderSettings.ConfigureBlazorHybridRenderModes();

var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

// Contenu pédagogique (chargé une fois) + rendu Markdown, partagés par toutes les pages.
builder.Services.AddSingleton<CourseCatalog>();
builder.Services.AddSingleton<MarkdownRenderer>();

// Layout piscine depuis l'environnement (même résolution que Piscine.DevHost / FromEnvironment) :
// contenu = PISCINE_CONTENT (sinon racine du catalogue), workspace/état sous PISCINE_HOME.
builder.Services.AddSingleton(sp =>
{
    var catalog = sp.GetRequiredService<CourseCatalog>();

    var content = Environment.GetEnvironmentVariable("PISCINE_CONTENT")
        ?? catalog.ContentRoot;

    var home = Environment.GetEnvironmentVariable("PISCINE_HOME")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "piscine");

    var workspace = Environment.GetEnvironmentVariable("PISCINE_WORKSPACE")
        ?? Path.Combine(home, "workspace");

    var state = Path.Combine(home, ".state");

    return new PiscineLayout(content, workspace, state);
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

builder.RootComponents.Add<Piscine.Desktop.App>("#app");

var app = builder.Build();

app.MainWindow
   .SetTitle("Piscine .NET")
   .SetUseOsDefaultSize(true);

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    app.MainWindow.ShowMessage("Erreur fatale", e.ExceptionObject.ToString());

app.Run();
