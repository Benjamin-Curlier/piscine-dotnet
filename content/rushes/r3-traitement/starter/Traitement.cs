// Rush 3 — Traitement de commandes (Worker Service)
// Lis N, puis N lignes "nom montant". Pousse chaque commande dans un Channel (file en mémoire),
// puis consomme la file dans un BackgroundService : accepte si montant > 0 (LogInformation),
// sinon rejette (LogWarning). À la fin, logue un bilan, puis arrête l'hôte.
//
// Le logger est FOURNI (LogCapture.cs) : utilise ILogger<Traitement> via l'injection de dépendances.
// Catégorie du log = nom de ta classe worker → sortie "Traitement [Information] ...".
//
// Squelette à compléter (les `using` nécessaires sont déjà là) :

using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// 1) Lis l'entrée et construis la liste des commandes (nom + montant).

// 2) Configure l'hôte : ClearProviders, AddProvider(new CaptureLoggerProvider()),
//    SetMinimumLevel(Information), AddFilter("Microsoft", None).
//    Enregistre l'entrée, le Validateur, et AddHostedService<Traitement>(). Puis host.Run().

// 3) Déclare les types APRÈS les instructions top-level :
//    record Commande(string Nom, int Montant);
//    record Entree(Commande[] Commandes);
//    class Validateur { bool EstAcceptee(Commande c) => c.Montant > 0; }
//    class Traitement : BackgroundService { ... consomme le Channel, logue, StopApplication() ... }
