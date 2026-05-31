using System.Linq;

var ligne = System.Console.ReadLine();
var t = ligne.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

var resultat = t switch
{
    [var x] => "un seul",
    [var f, .., var l] => $"premier={f} dernier={l}",
    _ => "?",
};

System.Console.WriteLine(resultat);
