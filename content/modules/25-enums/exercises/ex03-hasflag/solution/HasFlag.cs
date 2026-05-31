using System;

var combinee = (Permissions)int.Parse(System.Console.ReadLine());
var p = Enum.Parse<Permissions>(System.Console.ReadLine());
System.Console.WriteLine(combinee.HasFlag(p) ? "oui" : "non");

[Flags]
enum Permissions
{
    Lecture = 1,
    Ecriture = 2,
    Execution = 4,
}
