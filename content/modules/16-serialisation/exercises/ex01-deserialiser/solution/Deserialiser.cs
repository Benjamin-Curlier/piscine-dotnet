using System.Text.Json;

var ligne = System.Console.ReadLine();
var p = JsonSerializer.Deserialize<Personne>(ligne);
System.Console.WriteLine($"Nom: {p!.Nom}, Age: {p.Age}");

class Personne
{
    public string Nom { get; set; } = string.Empty;
    public int Age { get; set; }
}
