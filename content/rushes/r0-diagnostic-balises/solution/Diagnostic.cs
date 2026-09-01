var n = int.Parse(System.Console.ReadLine()!);
var names = new string[n];
var states = new string[n];

for (var i = 0; i < n; i++)
{
    var parts = System.Console.ReadLine()!.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
    names[i] = parts[0];
    states[i] = parts[1];
}

var ok = 0;
var warn = 0;
var ko = 0;
System.Console.WriteLine("INCIDENTS");
for (var i = 0; i < n; i++)
{
    if (states[i] == "OK")
    {
        ok++;
    }
    else if (states[i] == "WARN")
    {
        warn++;
        System.Console.WriteLine($"{names[i]}: WARN");
    }
    else
    {
        ko++;
        System.Console.WriteLine($"{names[i]}: KO");
    }
}

if (warn == 0 && ko == 0)
{
    System.Console.WriteLine("aucun");
}

System.Console.WriteLine($"Résumé: OK={ok} WARN={warn} KO={ko}");
