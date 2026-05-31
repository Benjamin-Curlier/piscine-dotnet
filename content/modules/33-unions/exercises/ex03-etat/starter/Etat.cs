using System;

// Lis "attente", "cours N" ou "termine X". Modélise l'état comme une union où CHAQUE
// variante porte ses propres données. Affiche : "en attente", "en cours a N%",
// "termine: X" selon l'état.

var ligne = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// TODO : construis l'Etat selon ligne[0], décris-le via un switch sur le type.

// TODO : abstract record Etat; sealed record EnAttente : Etat; sealed record EnCours(int Pourcent) : Etat; sealed record Termine(string Resultat) : Etat;
