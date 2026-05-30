var mot = System.Console.ReadLine();
var voyelles = 0;
foreach (var c in mot)
{
    if ("aeiou".Contains(char.ToLower(c)))
    {
        voyelles++;
    }
}
System.Console.WriteLine(voyelles);
