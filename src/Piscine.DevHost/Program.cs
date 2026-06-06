using Piscine.Components.Services;
using Piscine.DevHost.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Contenu pédagogique : chargé une fois au démarrage, partagé par toutes les requêtes.
builder.Services.AddSingleton<CourseCatalog>();
builder.Services.AddSingleton<MarkdownRenderer>();

// Lance des sessions PTY (un vrai shell OS) — sans état partagé, donc singleton.
builder.Services.AddSingleton<Piscine.App.Terminal.PtyService>();

// Boucle de coaching git : services purs (statut + règles, sans état) en singletons, et un canal
// named pipe singleton qui reçoit les événements du shim git pour la page /terminal.
builder.Services.AddSingleton<Piscine.App.Git.GitStatusService>();
builder.Services.AddSingleton<Piscine.App.Coaching.CoachingService>();
builder.Services.AddSingleton<Piscine.App.Coaching.ICoachingChannel, Piscine.App.Coaching.NamedPipeCoachingChannel>();

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
