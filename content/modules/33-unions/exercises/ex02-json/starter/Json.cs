using System;

// Lis "nombre N", "texte MOT" ou "booleen vrai|faux". Modélise une valeur JSON comme
// une union (Nombre/Texte/Booleen) et affiche-la : nombre tel quel, texte entre
// guillemets, booléen en true/false.

var ligne = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// TODO : construis la Valeur selon ligne[0], rends-la via un switch (avec déconstruction).

// TODO : abstract record Valeur; sealed record Nombre(int N) : Valeur; etc.
