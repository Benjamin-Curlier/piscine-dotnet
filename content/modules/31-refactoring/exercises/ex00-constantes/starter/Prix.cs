// Ce code FONCTIONNE mais il est truffé de nombres magiques (100, 10, 20).
// Refactore-le : donne un nom à chaque constante. Les tests doivent rester verts.

var prix = int.Parse(System.Console.ReadLine());

int remise;
if (prix > 100)
{
    remise = prix * 10 / 100;
}
else
{
    remise = 0;
}

var net = prix - remise;
var ttc = net + net * 20 / 100;

System.Console.WriteLine(ttc);
