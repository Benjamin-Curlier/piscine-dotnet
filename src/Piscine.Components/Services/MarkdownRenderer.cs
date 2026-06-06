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
        .Build();

    public MarkupString Render(string? markdown)
        => string.IsNullOrEmpty(markdown)
            ? default
            : new MarkupString(Markdown.ToHtml(markdown, _pipeline));
}
