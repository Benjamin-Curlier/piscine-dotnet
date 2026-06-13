using System;
using System.Threading;
using System.Threading.Tasks;

int n = int.Parse(System.Console.ReadLine());
long total = 0;
Parallel.For(1, n + 1, i =>
{
    Interlocked.Add(ref total, (long)i * i);
});
System.Console.WriteLine(total);
