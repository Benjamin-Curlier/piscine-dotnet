using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Piscine.App.Launch;
using Piscine.App.Settings;
using Piscine.Components.Services;
using Piscine.Core;
using ExercisePage = Piscine.Components.Components.Pages.Exercise;

namespace Piscine.Components.Tests;

public sealed class ExerciseTerminalActionTests : BunitContext
{
    private readonly string _tempHome;
    private readonly string _workspace;
    private readonly SettingsService _settings;
    private readonly RecordingProcessLauncher _processLauncher = new();

    public ExerciseTerminalActionTests()
    {
        var repoRoot = FindRepoRoot();
        var content = Path.Combine(repoRoot, "content");
        _tempHome = Path.Combine(Path.GetTempPath(), $"piscine-bunit-terminal-action-{Guid.NewGuid():N}");
        _workspace = Path.Combine(_tempHome, "workspace");
        var state = Path.Combine(_tempHome, ".state");
        Directory.CreateDirectory(_workspace);
        Directory.CreateDirectory(state);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PISCINE_CONTENT"] = content })
            .Build();
        var layout = new PiscineLayout(content, _workspace, state);
        _settings = new SettingsService(layout);

        Services.AddSingleton<IConfiguration>(configuration);
        Services.AddSingleton(new CourseCatalog(configuration));
        Services.AddSingleton(layout);
        Services.AddSingleton(_settings);
        Services.AddSingleton<IProcessLauncher>(_processLauncher);
        Services.AddSingleton<WorkspaceLauncher>();
        Services.AddSingleton<MarkdownRenderer>();
    }

    [Fact]
    public void DefaultEmbedded_PreparesStarterAndNavigatesToExerciseDirectory()
    {
        var cut = RenderExercise();

        cut.Find("[data-testid='ex-open-terminal']").Click();

        var expectedDirectory = Path.Combine(_workspace, "00-setup-git", "ex00-hello");
        Assert.True(File.Exists(Path.Combine(expectedDirectory, "Hello.cs")));
        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.Contains("/terminal?cwd=", navigation.Uri, StringComparison.Ordinal);
        Assert.Contains(
            expectedDirectory,
            Uri.UnescapeDataString(new Uri(navigation.Uri).Query),
            StringComparison.OrdinalIgnoreCase);
        Assert.Null(_processLauncher.Last);
    }

    [Fact]
    public void DefaultSystem_UsesSystemTerminalAndKeepsEmbeddedAlternative()
    {
        _settings.Save(new AppSettings { DefaultTerminal = TerminalTarget.System });
        var cut = RenderExercise();

        Assert.Contains(
            "terminal système",
            cut.Find("[data-testid='ex-open-terminal']").GetAttribute("title"),
            StringComparison.OrdinalIgnoreCase);
        cut.Find("[data-testid='ex-open-terminal-embedded']");
        cut.Find("[data-testid='ex-open-terminal']").Click();

        Assert.NotNull(_processLauncher.Last);
        Assert.Equal(
            OperatingSystem.IsWindows() ? "wt.exe" : "x-terminal-emulator",
            _processLauncher.Last!.FileName);
        Assert.Contains(
            Path.Combine(_workspace, "00-setup-git", "ex00-hello"),
            _processLauncher.Last.Arguments);
    }

    private IRenderedComponent<ExercisePage> RenderExercise() => Render<ExercisePage>(parameters => parameters
        .Add(component => component.ModuleId, "00-setup-git")
        .Add(component => component.ExerciseId, "ex00-hello"));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                Directory.Delete(_tempHome, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Piscine.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Piscine.slnx introuvable.");
    }

    private sealed class RecordingProcessLauncher : IProcessLauncher
    {
        public LaunchSpec? Last { get; private set; }

        public bool Launch(LaunchSpec spec)
        {
            Last = spec;
            return true;
        }
    }
}
