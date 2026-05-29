// Les graders qui exécutent du code redirigent Console.Out/In (état global du processus).
// La moulinette corrige de toute façon séquentiellement : on désactive la parallélisation
// des tests pour refléter ce modèle et éviter toute contamination croisée de la sortie.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
