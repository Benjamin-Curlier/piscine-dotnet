using System.Threading.Tasks;

var n = int.Parse(System.Console.ReadLine());
var resultat = await DoublerAsync(n);
System.Console.WriteLine(resultat);

static async Task<int> DoublerAsync(int x)
{
    await Task.Delay(1);
    return x * 2;
}
