using System;
using System.Linq;
using System.Text.Json;

string json = System.Console.ReadLine();
int[] nombres = JsonSerializer.Deserialize<int[]>(json);

var stats = new Stats
{
    Min = nombres.Min(),
    Max = nombres.Max(),
    Somme = nombres.Sum(),
};
System.Console.WriteLine(JsonSerializer.Serialize(stats));

sealed class Stats
{
    public int Min { get; set; }
    public int Max { get; set; }
    public int Somme { get; set; }
}
