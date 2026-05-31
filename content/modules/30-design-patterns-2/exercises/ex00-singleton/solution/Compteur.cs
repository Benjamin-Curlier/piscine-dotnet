// Deux "services" incrémentent le MÊME compteur partagé (singleton).
var a = int.Parse(System.Console.ReadLine());
var b = int.Parse(System.Console.ReadLine());

for (var i = 0; i < a; i++) { Compteur.Instance.Incrementer(); }
for (var i = 0; i < b; i++) { Compteur.Instance.Incrementer(); }

System.Console.WriteLine(Compteur.Instance.Valeur);

sealed class Compteur
{
    private static Compteur? _instance;

    // Unique point d'accès : la même instance pour tout le programme.
    public static Compteur Instance => _instance ??= new Compteur();

    // Constructeur privé : personne ne peut faire `new Compteur()` ailleurs.
    private Compteur() { }

    public int Valeur { get; private set; }

    public void Incrementer() => Valeur++;
}
