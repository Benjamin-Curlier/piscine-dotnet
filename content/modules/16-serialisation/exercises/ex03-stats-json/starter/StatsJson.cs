using System;
using System.Linq;
using System.Text.Json;

// L'entrée est UNE ligne : un tableau JSON d'entiers, ex. [3,1,4,1,5].
// Désérialise-le, calcule min / max / somme, et affiche un OBJET JSON sérialisé avec les propriétés
// Min, Max, Somme (noms par défaut, PascalCase), ex. {"Min":1,"Max":5,"Somme":14}.

string json = System.Console.ReadLine();
// TODO : désérialise json vers le tableau d'entiers à analyser.
int[] nombres = [];

// TODO : crée un objet (classe Stats { Min, Max, Somme }) et écris JsonSerializer.Serialize(...).
