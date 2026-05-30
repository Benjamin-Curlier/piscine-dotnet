using System;

var age = int.Parse(System.Console.ReadLine());

try
{
    Valider(age);
    System.Console.WriteLine($"Age: {age}");
}
catch (ArgumentOutOfRangeException)
{
    System.Console.WriteLine("Age invalide");
}

static void Valider(int age)
{
    if (age < 0 || age > 150)
    {
        throw new ArgumentOutOfRangeException(nameof(age));
    }
}
