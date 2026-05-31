using System;

// Lis "a/b". Modélise le résultat comme une union : Succes(valeur) OU Erreur(message).
// Si b != 0, renvoie Succes(a/b) ; sinon Erreur("division par zero").
// Affiche "OK <valeur>" pour un succès, "ERR <message>" pour une erreur.

var ligne = System.Console.ReadLine();

// TODO : parse a et b, construis le Resultat, affiche-le via un switch.

// TODO : abstract record Resultat; sealed record Succes(int Valeur) : Resultat; sealed record Erreur(string Message) : Resultat;
