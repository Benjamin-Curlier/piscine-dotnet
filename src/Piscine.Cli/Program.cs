using System.Reflection;
using Piscine.Core;

var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

Console.WriteLine(WelcomeBanner.Render(version));
Console.WriteLine();
Console.WriteLine("Itération 0 — squelette en place. Les commandes arrivent aux prochaines itérations.");
