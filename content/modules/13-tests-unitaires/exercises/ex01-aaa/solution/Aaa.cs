// Arrange : on prepare les donnees.
var a = int.Parse(System.Console.ReadLine());
var b = int.Parse(System.Console.ReadLine());
var attendu = int.Parse(System.Console.ReadLine());

// Act : on execute le calcul a tester.
var somme = a + b;

// Assert : on verifie le resultat.
if (somme == attendu)
{
    System.Console.WriteLine("PASS");
}
else
{
    System.Console.WriteLine("FAIL");
}
