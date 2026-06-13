using System;
using System.Text.RegularExpressions;

// Ligne 1 : un mot. Ligne 2 : un texte.
// Affiche le nombre d'occurrences du mot dans le texte, en tant que MOT ENTIER
// (limites de mot \b) et SANS tenir compte de la casse.
// Exemple : mot "le", texte "le chat et LE chien" -> 2.

string mot = System.Console.ReadLine();
string texte = System.Console.ReadLine();

// TODO : construis un Regex avec \b ... \b (échappe le mot avec Regex.Escape) et
// RegexOptions.IgnoreCase, puis compte les correspondances (Regex.Matches(...).Count).
