var n = int.Parse(System.Console.ReadLine());
if (n == 0)
{
    System.Console.WriteLine("0");
    return;
}

var resultat = string.Empty;
while (n > 0)
{
    resultat = (n % 2) + resultat;
    n /= 2;
}

System.Console.WriteLine(resultat);
