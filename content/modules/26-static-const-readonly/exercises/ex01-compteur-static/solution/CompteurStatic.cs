var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    new Objet();
}
System.Console.WriteLine(Objet.Count);

class Objet
{
    public static int Count;
    public Objet() { Count++; }
}
