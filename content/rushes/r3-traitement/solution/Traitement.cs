using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var n = int.Parse(System.Console.ReadLine()!);
var commandes = new List<Commande>(n);
for (var i = 0; i < n; i++)
{
    var morceaux = System.Console.ReadLine()!.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
    commandes.Add(new Commande(morceaux[0], int.Parse(morceaux[1])));
}

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddProvider(new CaptureLoggerProvider());
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.None);

builder.Services.AddSingleton(new Entree(commandes.ToArray()));
builder.Services.AddSingleton<Validateur>();
builder.Services.AddHostedService<Traitement>();

var host = builder.Build();
host.Run();

sealed record Commande(string Nom, int Montant);

sealed record Entree(Commande[] Commandes);

// Service métier injecté : décide de l'acceptation d'une commande.
sealed class Validateur
{
    public bool EstAcceptee(Commande commande) => commande.Montant > 0;
}

// Worker single-shot : consomme la file de commandes, journalise, dresse un bilan, puis arrête l'hôte.
sealed class Traitement : BackgroundService
{
    private readonly ILogger<Traitement> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Entree _entree;
    private readonly Validateur _validateur;

    public Traitement(
        ILogger<Traitement> logger,
        IHostApplicationLifetime lifetime,
        Entree entree,
        Validateur validateur)
    {
        _logger = logger;
        _lifetime = lifetime;
        _entree = entree;
        _validateur = validateur;
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken stoppingToken)
    {
        // File en mémoire : simule l'arrivée de commandes (à la place d'un vrai flux réseau).
        var file = Channel.CreateUnbounded<Commande>();

        foreach (var commande in _entree.Commandes)
        {
            await file.Writer.WriteAsync(commande, stoppingToken);
        }

        file.Writer.Complete();

        var acceptees = 0;
        var rejetees = 0;
        var total = 0;
        await foreach (var commande in file.Reader.ReadAllAsync(stoppingToken))
        {
            if (_validateur.EstAcceptee(commande))
            {
                acceptees++;
                total += commande.Montant;
                _logger.LogInformation("Commande acceptée : {Nom} ({Montant})", commande.Nom, commande.Montant);
            }
            else
            {
                rejetees++;
                _logger.LogWarning("Commande rejetée : {Nom}", commande.Nom);
            }
        }

        _logger.LogInformation(
            "Bilan : {Acceptees} acceptée(s), {Rejetees} rejetée(s), total {Total}", acceptees, rejetees, total);
        _lifetime.StopApplication();
    }
}
