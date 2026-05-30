using System.Text.Json;
using System.Collections.Generic;

var n = int.Parse(System.Console.ReadLine());
var liste = new List<string>();
for (var i = 0; i < n; i++)
{
    liste.Add(System.Console.ReadLine());
}

var json = JsonSerializer.Serialize(liste);
System.Console.WriteLine(json);
