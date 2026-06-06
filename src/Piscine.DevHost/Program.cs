using Piscine.App.Checking;
using Piscine.App.Progress;
using Piscine.Components.Services;
using Piscine.Core;
using Piscine.DevHost.Components;
using Piscine.Grading;
using Piscine.App.Init;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Contenu pédagogique : chargé une fois au démarrage, partagé par toutes les requêtes.
builder.Services.AddSingleton<CourseCatalog>();
builder.Services.AddSingleton<MarkdownRenderer>();

// Layout piscine : contenu depuis PISCINE_CONTENT (ou l'appsettings), workspace depuis
// PISCINE_WORKSPACE (pour permettre à l'E2E de pointer vers un workspace isolé), état depuis
// PISCINE_HOME (comme FromEnvironment).  Construit explicitement pour être controlable par env.
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

// Service de correction in-process (pur, sans git ni progression) : singleton sans état.
builder.Services.AddSingleton(sp =>
    new CheckService(sp.GetRequiredService<PiscineLayout>(), Graders.Default()));

// Lance des sessions PTY (un vrai shell OS) — sans état partagé, donc singleton.
builder.Services.AddSingleton<Piscine.App.Terminal.PtyService>();

// Boucle de coaching git : services purs (statut + règles, sans état) en singletons, et un canal
// named pipe singleton qui reçoit les événements du shim git pour la page /terminal.
builder.Services.AddSingleton<Piscine.App.Git.GitStatusService>();
builder.Services.AddSingleton<Piscine.App.Coaching.CoachingService>();
builder.Services.AddSingleton<Piscine.App.Coaching.ICoachingChannel, Piscine.App.Coaching.NamedPipeCoachingChannel>();

// Service d'initialisation : enrobe GitWorkspace.Initialize, chemin exe piscine surclassable par PISCINE_EXE.
builder.Services.AddSingleton(sp =>
{
    var layout = sp.GetRequiredService<PiscineLayout>();
    var exe = Environment.GetEnvironmentVariable("PISCINE_EXE") ?? "piscine";
    return new InitService(layout, exe);
});

// Vue de progression : lit progress.json + RepoState + workspace (lecture seule).
builder.Services.AddSingleton(sp => new ProgressService(
    sp.GetRequiredService<PiscineLayout>(),
    sp.GetRequiredService<Piscine.App.Git.GitStatusService>()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // Les composants routables (@page) vivent dans la RCL Piscine.Components :
    // il faut déclarer son assembly pour que le routage serveur les découvre.
    .AddAdditionalAssemblies(typeof(Piscine.Components.MarkdownView).Assembly);

app.Run();
