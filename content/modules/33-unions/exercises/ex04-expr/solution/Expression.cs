using System;

// Union RÉCURSIVE : un arbre d'expression arithmétique évalué par pattern matching.
// Entrée en notation polonaise préfixe, ex. "+ 3 * 4 2" => 3 + (4 * 2) = 11.
var tokens = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

var position = 0;
var arbre = Lire(tokens, ref position);
System.Console.WriteLine(Evaluer(arbre));

static Expr Lire(string[] tokens, ref int pos)
{
    var t = tokens[pos];
    pos++;
    if (t == "+" || t == "*")
    {
        var gauche = Lire(tokens, ref pos);
        var droite = Lire(tokens, ref pos);
        return new Operation(t, gauche, droite);
    }
    return new Nombre(int.Parse(t));
}

static int Evaluer(Expr e) => e switch
{
    Nombre n => n.Valeur,
    Operation o when o.Symbole == "+" => Evaluer(o.Gauche) + Evaluer(o.Droite),
    Operation o => Evaluer(o.Gauche) * Evaluer(o.Droite),
    _ => 0
};

abstract record Expr;
sealed record Nombre(int Valeur) : Expr;
sealed record Operation(string Symbole, Expr Gauche, Expr Droite) : Expr;
