var n = int.Parse(System.Console.ReadLine());
var t = System.Array.ConvertAll(
    System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries),
    int.Parse);

for (var i = 0; i < n - 1; i++)
{
    for (var j = 0; j < n - 1 - i; j++)
    {
        if (t[j] > t[j + 1])
        {
            var tmp  = t[j];
            t[j]     = t[j + 1];
            t[j + 1] = tmp;
        }
    }
}

System.Console.WriteLine(string.Join(" ", t));
