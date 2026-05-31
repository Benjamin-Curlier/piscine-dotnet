// Version refactorée : la "god function" est découpée en étapes lisibles (parser, calculer, formater).
var ligne = System.Console.ReadLine();

var (nom, notes) = Parser(ligne);
var moyenne = Moyenne(notes);
System.Console.WriteLine(Formater(nom, moyenne));

static (string nom, int[] notes) Parser(string ligne)
{
    var champs = ligne.Split(',');
    var notes = new int[champs.Length - 1];
    for (var i = 0; i < notes.Length; i++)
    {
        notes[i] = int.Parse(champs[i + 1]);
    }
    return (champs[0], notes);
}

static int Moyenne(int[] notes)
{
    var somme = 0;
    foreach (var n in notes)
    {
        somme += n;
    }
    return somme / notes.Length;
}

static string Formater(string nom, int moyenne) => nom + ": " + moyenne;
