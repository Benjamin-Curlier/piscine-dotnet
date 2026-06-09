using Markdig;
using Microsoft.AspNetCore.Components;

namespace Piscine.Components.Services;

/// <summary>Convertit le markdown du cours en HTML (avec extensions GitHub et ancres d'en-têtes).</summary>
public sealed class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseAutoIdentifiers(Markdig.Extensions.AutoIdentifiers.AutoIdentifierOptions.GitHub)
        .UseSoftlineBreakAsHardlineBreak()
        // Le HTML brut du markdown est ÉCHAPPÉ (rendu en texte), pas réinjecté tel quel : le rendu est
        // enveloppé dans une MarkupString non encodée et affiché dans le WebView, donc un cours tiers
        // contenant <script>/<img onerror=…> exécuterait du code sans cette garde. Le contenu officiel
        // n'utilise aucun HTML brut, donc aucun rendu légitime n'est affecté.
        .DisableHtml()
        .Build();

    public MarkupString Render(string? markdown)
        => string.IsNullOrEmpty(markdown)
            ? default
            : new MarkupString(Markdown.ToHtml(markdown, _pipeline));
}
