using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using Piscine.Components.Services;

var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

// MarkdownView dépend de MarkdownRenderer via DI : on l'enregistre.
// (Pas de CourseCatalog : ce spike n'utilise pas la découverte de contenu.)
builder.Services.AddSingleton<MarkdownRenderer>();

builder.RootComponents.Add<Piscine.Desktop.App>("#app");

var app = builder.Build();

app.MainWindow
   .SetTitle("Piscine .NET")
   .SetUseOsDefaultSize(true);

AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    app.MainWindow.ShowMessage("Erreur fatale", e.ExceptionObject.ToString());

app.Run();
