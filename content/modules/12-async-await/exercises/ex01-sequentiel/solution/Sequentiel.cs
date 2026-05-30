using System.Threading.Tasks;

var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    var valeur = int.Parse(System.Console.ReadLine());
    var carre = await CarreAsync(valeur);
    System.Console.WriteLine(carre);
}

static async Task<int> CarreAsync(int x)
{
    await Task.Delay(1);
    return x * x;
}
