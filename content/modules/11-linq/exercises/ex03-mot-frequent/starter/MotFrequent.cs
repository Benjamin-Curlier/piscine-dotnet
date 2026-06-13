using System;
using System.Linq;
using System.Collections.Generic;

// Lis des mots (un par ligne) jusqu'à la fin de l'entrée. Affiche le mot le PLUS fréquent et son
// nombre d'occurrences, au format « <mot> <nombre> ». En cas d'égalité de fréquence, choisis le mot
// le plus petit dans l'ordre alphabétique (ordinal) — la sortie doit être DÉTERMINISTE.
// CONTRAINTE : utilise LINQ (GroupBy / OrderByDescending / ThenBy / First).

var mots = new List<string>();
string ligne;
while ((ligne = System.Console.ReadLine()) is not null && ligne.Length > 0)
{
    mots.Add(ligne.Trim());
}

// TODO : groupe par mot, trie par fréquence décroissante puis par clé (ordinal), prends le premier.
