using System;

var n = int.Parse(System.Console.ReadLine());
Permissions resultat = default;
for (var i = 0; i < n; i++)
{
    var nom = System.Console.ReadLine();
    resultat |= Enum.Parse<Permissions>(nom);
}

System.Console.WriteLine((int)resultat);

[Flags]
enum Permissions
{
    Lecture = 1,
    Ecriture = 2,
    Execution = 4,
}
