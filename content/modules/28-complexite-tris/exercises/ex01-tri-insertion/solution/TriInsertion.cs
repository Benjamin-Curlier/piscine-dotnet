var n = int.Parse(System.Console.ReadLine());
var t = System.Array.ConvertAll(
    System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries),
    int.Parse);

for (var i = 1; i < n; i++)
{
    var cle = t[i];
    var j   = i - 1;
    while (j >= 0 && t[j] > cle)
    {
        t[j + 1] = t[j];
        j--;
    }
    t[j + 1] = cle;
}

System.Console.WriteLine(string.Join(" ", t));
