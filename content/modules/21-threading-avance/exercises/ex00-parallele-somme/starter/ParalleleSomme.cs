// Lis N, puis calcule 1+2+...+N en parallèle avec Parallel.For.
// Astuce : long total = 0; Parallel.For(1, n + 1, i => Interlocked.Add(ref total, i));
