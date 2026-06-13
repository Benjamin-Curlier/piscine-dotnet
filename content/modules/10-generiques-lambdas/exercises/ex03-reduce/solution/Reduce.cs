using System;
using System.Collections.Generic;

var nombres = new List<int>();
string ligne;
while ((ligne = System.Console.ReadLine()) is not null && ligne.Length > 0)
{
    nombres.Add(int.Parse(ligne));
}

int somme = Reduce(nombres, 0, (acc, x) => acc + x);
int produit = Reduce(nombres, 1, (acc, x) => acc * x);
System.Console.WriteLine($"somme={somme}");
System.Console.WriteLine($"produit={produit}");

// Réduction générique (fold) : accumule les éléments via une fonction (acc, élément) -> acc.
static R Reduce<T, R>(IEnumerable<T> source, R seed, Func<R, T, R> combiner)
{
    R acc = seed;
    foreach (T element in source)
    {
        acc = combiner(acc, element);
    }
    return acc;
}
