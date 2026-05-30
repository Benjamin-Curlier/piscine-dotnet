var mot = System.Console.ReadLine();
var nombre = int.Parse(System.Console.ReadLine());

var boiteMot = new Boite<string>(mot);
var boiteNombre = new Boite<int>(nombre);

System.Console.WriteLine(boiteMot.Decrire());
System.Console.WriteLine(boiteNombre.Decrire());

class Boite<T>
{
    public T Contenu { get; }
    public Boite(T contenu) => Contenu = contenu;
    public string Decrire() => $"Boite contient: {Contenu}";
}
