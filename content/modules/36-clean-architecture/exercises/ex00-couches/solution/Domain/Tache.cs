namespace Domain;

// Entité du domaine : une tâche. Logique métier pure, aucune dépendance technique.
public sealed class Tache
{
    public Tache(int id, string titre)
    {
        Id = id;
        Titre = titre;
    }

    public int Id { get; }

    public string Titre { get; }

    public bool Faite { get; private set; }

    public void Terminer() => Faite = true;
}
