using System.Threading;
using System.Threading.Tasks;

var n = int.Parse(System.Console.ReadLine());

long total = 0;
System.Threading.Tasks.Parallel.For(1, n + 1, i => System.Threading.Interlocked.Add(ref total, i));

System.Console.WriteLine(total);
