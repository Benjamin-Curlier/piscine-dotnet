var nom = System.Console.ReadLine();
var age = int.Parse(System.Console.ReadLine());
var personne = new Personne { Nom = nom, Age = age };
System.Console.WriteLine(personne.SePresenter());

class Personne
{
    public string Nom { get; set; } = string.Empty;
    public int Age { get; set; }
    public string SePresenter() => $"Je m'appelle {Nom}, j'ai {Age} ans.";
}
