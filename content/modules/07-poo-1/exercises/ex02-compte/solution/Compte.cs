var initial = int.Parse(System.Console.ReadLine());
var depot = int.Parse(System.Console.ReadLine());
var retrait = int.Parse(System.Console.ReadLine());

var compte = new CompteBancaire(initial);
compte.Deposer(depot);
compte.Retirer(retrait);
System.Console.WriteLine(compte.Solde);

class CompteBancaire
{
    private int _solde;
    public CompteBancaire(int soldeInitial) => _solde = soldeInitial;

    public int Solde => _solde;
    public void Deposer(int montant) => _solde += montant;
    public void Retirer(int montant)
    {
        if (montant <= _solde)
        {
            _solde -= montant;
        }
    }
}
