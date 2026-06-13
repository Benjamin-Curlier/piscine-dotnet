using System;
using System.Linq;
using System.Reflection;

// Lis Nom (ligne 1), Age (ligne 2, entier), Actif (ligne 3, true/false) et construis un objet
// Personne { Nom, Age, Actif }.
// Puis, par RÉFLEXION (sans écrire les noms à la main), affiche chaque propriété au format
// « Nom=valeur », triées par nom (ordre ordinal) — donc Actif, puis Age, puis Nom.

var personne = new Personne
{
    Nom = System.Console.ReadLine(),
    Age = int.Parse(System.Console.ReadLine()),
    Actif = bool.Parse(System.Console.ReadLine()),
};

// TODO : personne.GetType().GetProperties(), trie par .Name (StringComparer.Ordinal),
// et pour chacune affiche $"{prop.Name}={prop.GetValue(personne)}".

sealed class Personne
{
    public string Nom { get; set; } = "";
    public int Age { get; set; }
    public bool Actif { get; set; }
}
