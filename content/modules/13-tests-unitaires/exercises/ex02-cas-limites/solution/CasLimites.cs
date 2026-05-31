var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    var valeur = int.Parse(System.Console.ReadLine());
    if (valeur > 0)
    {
        System.Console.WriteLine("positif");
    }
    else if (valeur < 0)
    {
        System.Console.WriteLine("negatif");
    }
    else
    {
        System.Console.WriteLine("zero");
    }
}
