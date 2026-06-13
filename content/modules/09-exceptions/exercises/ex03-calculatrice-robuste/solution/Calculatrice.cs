using System;

int n = int.Parse(System.Console.ReadLine());
for (int i = 1; i <= n; i++)
{
    string ligne = System.Console.ReadLine();
    try
    {
        string[] parts = ligne.Split(' ');
        int a = int.Parse(parts[0]);
        string op = parts[1];
        int b = int.Parse(parts[2]);
        int r = op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" => a / b,
            _ => throw new InvalidOperationException(),
        };
        System.Console.WriteLine($"{i}: {r}");
    }
    catch (FormatException)
    {
        System.Console.WriteLine($"{i}: erreur de format");
    }
    catch (DivideByZeroException)
    {
        System.Console.WriteLine($"{i}: division par zero");
    }
    catch (InvalidOperationException)
    {
        System.Console.WriteLine($"{i}: operateur inconnu");
    }
}
