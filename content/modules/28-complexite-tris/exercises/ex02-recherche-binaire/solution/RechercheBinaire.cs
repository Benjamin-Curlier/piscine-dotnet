var n     = int.Parse(System.Console.ReadLine());
var t     = System.Array.ConvertAll(
    System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries),
    int.Parse);
var cible = int.Parse(System.Console.ReadLine());

var gauche = 0;
var droite = n - 1;
var indice = -1;

while (gauche <= droite)
{
    var milieu = (gauche + droite) / 2;
    if (t[milieu] == cible)
    {
        indice = milieu;
        break;
    }
    else if (t[milieu] < cible)
    {
        gauche = milieu + 1;
    }
    else
    {
        droite = milieu - 1;
    }
}

System.Console.WriteLine(indice);
