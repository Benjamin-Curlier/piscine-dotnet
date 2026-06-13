using System;
using System.Threading;
using System.Threading.Tasks;

// Lis N. Calcule la somme des carrés 1²+2²+...+N² EN PARALLÈLE (Parallel.For), en accumulant dans
// un total partagé SANS race condition (Interlocked.Add sur un long). Affiche le total.
// Le résultat doit être DÉTERMINISTE malgré le parallélisme.

int n = int.Parse(System.Console.ReadLine());
long total = 0;

// TODO : Parallel.For(1, n + 1, i => Interlocked.Add(ref total, (long)i * i));
// TODO : affiche total.
