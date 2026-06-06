using Piscine.Components.Services;
using Piscine.DevHost.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Contenu pédagogique : chargé une fois au démarrage, partagé par toutes les requêtes.
builder.Services.AddSingleton<CourseCatalog>();
builder.Services.AddSingleton<MarkdownRenderer>();

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
