// Les graders manipulent la Console globale (redirection stdout) : on corrige
// séquentiellement pour éviter les courses entre tests.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]
