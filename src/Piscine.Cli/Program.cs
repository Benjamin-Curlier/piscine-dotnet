using System.Reflection;
using Piscine.Core;
using Piscine.Core.Content;
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

    default:
        Console.WriteLine($"Commande inconnue : {command}");
        Console.WriteLine("Commandes : list | start <exo> | check <exo> | status");
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
