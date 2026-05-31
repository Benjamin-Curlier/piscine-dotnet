using System;
using System.Reflection;

var etiquette = typeof(MaClasse).GetCustomAttribute<EtiquetteAttribute>()!;
System.Console.WriteLine(etiquette.Texte);

class EtiquetteAttribute : Attribute
{
    public string Texte { get; }
    public EtiquetteAttribute(string texte) => Texte = texte;
}

[Etiquette("Coucou")]
class MaClasse { }
