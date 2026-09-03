// Deux "services" incrémentent le MÊME compteur partagé (singleton).
var a = int.Parse(System.Console.ReadLine());
var b = int.Parse(System.Console.ReadLine());

for (var i = 0; i < a; i++) { Compteur.Instance.Incrementer(); }
for (var i = 0; i < b; i++) { Compteur.Instance.Incrementer(); }

System.Console.WriteLine(Compteur.Instance.Valeur);

sealed class Compteur
{
    // Unique point d'accès, initialisé de façon sûre par le runtime .NET.
    public static Compteur Instance { get; } = new Compteur();

    // Constructeur privé : personne ne peut faire `new Compteur()` ailleurs.
    private Compteur() { }

    public int Valeur { get; private set; }

    public void Incrementer() => Valeur++;
}
