// Ce code FONCTIONNE mais tout est entassé dans le flux principal (parsing + calcul + format).
// Refactore-le en méthodes Parser / Moyenne / Formater. Les tests doivent rester verts.

var ligne = System.Console.ReadLine();
var champs = ligne.Split(',');
var nom = champs[0];

var somme = 0;
for (var i = 1; i < champs.Length; i++)
{
    somme += int.Parse(champs[i]);
}

var moyenne = somme / (champs.Length - 1);

System.Console.WriteLine(nom + ": " + moyenne);
