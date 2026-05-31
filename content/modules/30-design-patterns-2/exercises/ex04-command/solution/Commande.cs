using System.Collections.Generic;

var calc = new Calculatrice();
var historique = new Stack<ICommande>();

string? ligne;
while ((ligne = System.Console.ReadLine()) != null)
{
    if (ligne.Length == 0) { continue; }
    var parts = ligne.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

    if (parts[0] == "undo")
    {
        if (historique.Count > 0)
        {
            historique.Pop().Annuler(calc);
        }
        continue;
    }

    var n = int.Parse(parts[1]);
    ICommande cmd = parts[0] == "add" ? new Ajouter(n) : new Soustraire(n);
    cmd.Executer(calc);
    historique.Push(cmd);
}

System.Console.WriteLine(calc.Valeur);

sealed class Calculatrice
{
    public int Valeur { get; set; }
}

// Chaque commande sait s'exécuter ET s'annuler : c'est ce qui permet le undo.
interface ICommande
{
    void Executer(Calculatrice c);
    void Annuler(Calculatrice c);
}

sealed class Ajouter : ICommande
{
    private readonly int _n;
    public Ajouter(int n) => _n = n;
    public void Executer(Calculatrice c) => c.Valeur += _n;
    public void Annuler(Calculatrice c) => c.Valeur -= _n;
}

sealed class Soustraire : ICommande
{
    private readonly int _n;
    public Soustraire(int n) => _n = n;
    public void Executer(Calculatrice c) => c.Valeur -= _n;
    public void Annuler(Calculatrice c) => c.Valeur += _n;
}
