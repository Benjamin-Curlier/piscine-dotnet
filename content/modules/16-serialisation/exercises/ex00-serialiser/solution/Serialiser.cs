using System.Text.Json;

var nom = System.Console.ReadLine();
var age = int.Parse(System.Console.ReadLine());

var p = new Personne { Nom = nom, Age = age };
var json = JsonSerializer.Serialize(p);
System.Console.WriteLine(json);

class Personne
{
    public string Nom { get; set; } = string.Empty;
    public int Age { get; set; }
}
