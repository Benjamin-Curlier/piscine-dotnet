namespace Domain;

// Entité du domaine : un livre, avec sa règle métier (on ne prête pas un livre déjà emprunté).
public sealed class Livre
{
    public Livre(int id, string titre)
    {
        Id = id;
        Titre = titre;
    }

    public int Id { get; }

    public string Titre { get; }

    public bool Emprunte { get; private set; }

    public void Emprunter() => Emprunte = true;

    public void Rendre() => Emprunte = false;
}
