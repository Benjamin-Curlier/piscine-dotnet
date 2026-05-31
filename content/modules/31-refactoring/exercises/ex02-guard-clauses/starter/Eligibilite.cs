// Ce code FONCTIONNE mais les if imbriqués sont pénibles à lire.
// Refactore-le avec des clauses-gardes (retours anticipés). Les tests doivent rester verts.

var age = int.Parse(System.Console.ReadLine());
var solde = int.Parse(System.Console.ReadLine());

string r;
if (age >= 18)
{
    if (solde >= 0)
    {
        if (solde <= 100000)
        {
            r = "OUI";
        }
        else
        {
            r = "NON";
        }
    }
    else
    {
        r = "NON";
    }
}
else
{
    r = "NON";
}

System.Console.WriteLine(r);
