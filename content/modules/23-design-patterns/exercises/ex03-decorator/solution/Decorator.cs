using System;

string texte = System.Console.ReadLine();
string decorations = System.Console.ReadLine();

ITexte resultat = new TexteBrut(texte);
foreach (string deco in decorations.Split(','))
{
    resultat = deco switch
    {
        "maj" => new Majuscule(resultat),
        "crochets" => new Crochets(resultat),
        "etoiles" => new Etoiles(resultat),
        _ => resultat,
    };
}
System.Console.WriteLine(resultat.Rendu());

interface ITexte
{
    string Rendu();
}

sealed class TexteBrut : ITexte
{
    private readonly string _texte;
    public TexteBrut(string texte) => _texte = texte;
    public string Rendu() => _texte;
}

sealed class Majuscule : ITexte
{
    private readonly ITexte _inner;
    public Majuscule(ITexte inner) => _inner = inner;
    public string Rendu() => _inner.Rendu().ToUpperInvariant();
}

sealed class Crochets : ITexte
{
    private readonly ITexte _inner;
    public Crochets(ITexte inner) => _inner = inner;
    public string Rendu() => $"[{_inner.Rendu()}]";
}

sealed class Etoiles : ITexte
{
    private readonly ITexte _inner;
    public Etoiles(ITexte inner) => _inner = inner;
    public string Rendu() => $"*{_inner.Rendu()}*";
}
