using System.Reflection;
using Piscine.Core;
using Piscine.Core.Content;
using Piscine.Git;
using Piscine.Grading;

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
var layout = PiscineLayout.FromEnvironment();

var command = args.Length > 0 ? args[0] : "status";

switch (command)
{
    case "list":
        ListModules(layout);
        return 0;

    case "start":
        return Start(layout, args);

    case "check":
        return Check(layout, args);

    case "status":
        Status(version, layout);
        return 0;

    case "init":
        return Init(layout);

    case "grade-received":
        return GradeReceived(layout, args);

    case "validate-content":
        return ValidateContent(layout);

    default:
        Console.WriteLine($"Commande inconnue : {command}");
        Console.WriteLine("Commandes : list | start <exo> | check <exo> | status | init | grade-received <sha> | validate-content");
        return 64;
}

static void ListModules(PiscineLayout layout)
{
    var modules = ContentDiscovery.DiscoverModules(layout.Content);
    if (modules.Count == 0)
    {
        Console.WriteLine("Aucun module disponible pour le moment.");
        return;
    }

    foreach (var module in modules)
    {
        Console.WriteLine($"# {module.Id} — {module.Title}");
        foreach (var group in module.Groups)
        {
            Console.WriteLine($"  {group.Title}");
            foreach (var exercise in group.Exercises)
            {
                Console.WriteLine($"    - {exercise}");
            }
        }
    }
}

static int Start(PiscineLayout layout, string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage : piscine start <exo>");
        return 64;
    }

    var exerciseId = args[1];
    var location = ContentLocator.FindExercise(layout.Content, exerciseId);
    if (location is null)
    {
        Console.WriteLine($"Exercice introuvable : {exerciseId}");
        return 2;
    }

    var workspaceDir = layout.WorkspaceExerciseDir(location.ModuleId, exerciseId);
    StarterInstaller.Install(location.ContentDir, workspaceDir);

    var subject = System.IO.Path.Combine(location.ContentDir, "subject.md");
    if (System.IO.File.Exists(subject))
    {
        Console.WriteLine(System.IO.File.ReadAllText(subject));
    }

    Console.WriteLine();
    Console.WriteLine($"Exercice prêt dans : {workspaceDir}");
    Console.WriteLine($"Quand tu as codé : piscine check {exerciseId}");
    return 0;
}

static int Check(PiscineLayout layout, string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage : piscine check <exo>");
        return 64;
    }

    var result = new CheckCommand(layout, Graders.Default()).Run(args[1]);
    Console.WriteLine(result.Output);
    return result.ExitCode;
}

static int ValidateContent(PiscineLayout layout)
{
    var report = new ContentValidator(Graders.Default()).Validate(layout);
    if (report.IsValid)
    {
        Console.WriteLine("Contenu valide.");
        return 0;
    }

    foreach (var issue in report.Issues)
    {
        Console.WriteLine($"[{issue.ExerciseId}] {issue.Message}");
    }

    Console.WriteLine($"{report.Issues.Count} problème(s) de contenu.");
    return 1;
}

static int Init(PiscineLayout layout)
{
    var exe = Environment.ProcessPath ?? "piscine";
    GitWorkspace.Initialize(layout, exe);
    Console.WriteLine("Piscine initialisée.");
    Console.WriteLine($"  workspace : {layout.WorkspaceRoot}");
    Console.WriteLine($"  origin    : {layout.RemoteRepoPath}");
    Console.WriteLine("Travaille dans le workspace, puis : git add/commit/push origin main");
    return 0;
}

static int GradeReceived(PiscineLayout layout, string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage : piscine grade-received <sha>");
        return 64;
    }

    var result = new GradeReceivedCommand(layout, Graders.Default()).Run(args[1]);
    Console.WriteLine(result.Output);
    return result.ExitCode;
}

static void Status(string version, PiscineLayout layout)
{
    Console.WriteLine(WelcomeBanner.Render(version));
    Console.WriteLine();

    var modules = ContentDiscovery.DiscoverModules(layout.Content);
    if (modules.Count == 0)
    {
        Console.WriteLine("Aucun module installé. (Le contenu arrivera dans une prochaine itération.)");
        return;
    }

    Console.WriteLine($"{modules.Count} module(s) disponible(s). Tape 'piscine list' pour les voir.");
}
