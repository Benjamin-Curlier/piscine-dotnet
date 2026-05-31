using System;

// Lis "cercle R", "rectangle L H" ou "carre C". Modélise Forme comme une hiérarchie
// SCELLÉE (abstract record + sealed records), construis la bonne variante, puis calcule
// l'aire par un switch sur le type. Pour le cercle, prends pi = 3 (aire = 3*R*R).

var ligne = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// TODO : construis la Forme selon ligne[0], calcule l'aire par pattern matching, affiche-la.

// TODO : abstract record Forme; sealed record Cercle(int Rayon) : Forme; etc.
