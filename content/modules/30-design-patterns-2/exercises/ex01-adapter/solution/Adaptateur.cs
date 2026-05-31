var donnees = System.Console.ReadLine();
var nom = System.Console.ReadLine();

// Le client ne connaît que IAnnuaire ; l'adaptateur l'alimente avec l'API legacy.
IAnnuaire annuaire = new AnnuaireAdapter(new AnnuaireLegacy(donnees));
System.Console.WriteLine(annuaire.Age(nom));

interface IAnnuaire
{
    int Age(string nom);
}

// API "héritée" qu'on ne peut pas modifier : tout est dans une seule chaîne.
sealed class AnnuaireLegacy
{
    private readonly string _data;

    public AnnuaireLegacy(string data) => _data = data;

    public string Tout() => _data;
}

// Adaptateur : implémente IAnnuaire en s'appuyant sur AnnuaireLegacy.
sealed class AnnuaireAdapter : IAnnuaire
{
    private readonly AnnuaireLegacy _legacy;

    public AnnuaireAdapter(AnnuaireLegacy legacy) => _legacy = legacy;

    public int Age(string nom)
    {
        foreach (var paire in _legacy.Tout().Split(','))
        {
            var kv = paire.Split(':');
            if (kv[0] == nom)
            {
                return int.Parse(kv[1]);
            }
        }

        return -1;
    }
}
