using System;
using System.Text.RegularExpressions;

string mot = System.Console.ReadLine();
string texte = System.Console.ReadLine();

int n = Regex.Matches(texte, $@"\b{Regex.Escape(mot)}\b", RegexOptions.IgnoreCase).Count;
System.Console.WriteLine(n);
