using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

int n = int.Parse(System.Console.ReadLine());
var taches = new List<Task<int>>();
for (int i = 0; i < n; i++)
{
    int x = int.Parse(System.Console.ReadLine());
    taches.Add(CarreAsync(x));
}

int[] carres = await Task.WhenAll(taches);
System.Console.WriteLine($"somme des carres = {carres.Sum()}");

// Calcule le carré de façon asynchrone. Task.Yield rend la méthode réellement asynchrone
// sans introduire de délai : le résultat reste déterministe.
static async Task<int> CarreAsync(int x)
{
    await Task.Yield();
    return x * x;
}
