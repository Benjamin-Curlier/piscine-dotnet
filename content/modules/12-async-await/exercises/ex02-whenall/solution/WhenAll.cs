using System.Threading.Tasks;

var n = int.Parse(System.Console.ReadLine());
var tasks = new Task<int>[n];
for (var i = 0; i < n; i++)
{
    var valeur = int.Parse(System.Console.ReadLine());
    tasks[i] = CarreAsync(valeur);
}

var resultats = await Task.WhenAll(tasks);
for (var i = 0; i < resultats.Length; i++)
{
    System.Console.WriteLine(resultats[i]);
}

static async Task<int> CarreAsync(int x)
{
    await Task.Delay(1);
    return x * x;
}
