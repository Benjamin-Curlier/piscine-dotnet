public class Compte
{
    public int Solde { get; private set; } = 100;

    public bool Retirer(int montant)
    {
        if (montant <= Solde)
        {
            Solde -= montant;
            return true;
        }

        return false;
    }
}
