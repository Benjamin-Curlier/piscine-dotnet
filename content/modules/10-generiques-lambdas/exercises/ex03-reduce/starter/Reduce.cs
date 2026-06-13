using System;
using System.Collections.Generic;

// Lis des entiers (un par ligne) jusqu'à la fin de l'entrée. Puis affiche :
//   somme=<somme des entiers>
//   produit=<produit des entiers>
// CONTRAINTE : calcule somme ET produit avec UNE SEULE méthode générique `Reduce<T, R>`
// (un « fold ») + deux lambdas. La somme part de 0, le produit de 1.

var nombres = new List<int>();
string ligne;
while ((ligne = System.Console.ReadLine()) is not null && ligne.Length > 0)
{
    nombres.Add(int.Parse(ligne));
}

// TODO : int somme = Reduce(nombres, 0, (acc, x) => ...);
// TODO : int produit = Reduce(nombres, 1, (acc, x) => ...);
// TODO : affiche les deux lignes.

// TODO : écris la méthode générique
// static R Reduce<T, R>(IEnumerable<T> source, R seed, Func<R, T, R> combiner) { ... }
