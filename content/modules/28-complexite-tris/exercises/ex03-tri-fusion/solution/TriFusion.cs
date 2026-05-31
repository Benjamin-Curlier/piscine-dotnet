var n = int.Parse(System.Console.ReadLine());
var t = System.Array.ConvertAll(
    System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries),
    int.Parse);

t = TriFusion(t);
System.Console.WriteLine(string.Join(" ", t));

static int[] TriFusion(int[] t)
{
    if (t.Length <= 1) return t;

    var milieu = t.Length / 2;
    var gauche = new int[milieu];
    var droite = new int[t.Length - milieu];

    System.Array.Copy(t, 0, gauche, 0, milieu);
    System.Array.Copy(t, milieu, droite, 0, droite.Length);

    gauche = TriFusion(gauche);
    droite = TriFusion(droite);

    return Fusionner(gauche, droite);
}

static int[] Fusionner(int[] g, int[] d)
{
    var resultat = new int[g.Length + d.Length];
    int i = 0, j = 0, k = 0;
    while (i < g.Length && j < d.Length)
    {
        if (g[i] <= d[j]) resultat[k++] = g[i++];
        else              resultat[k++] = d[j++];
    }
    while (i < g.Length) resultat[k++] = g[i++];
    while (j < d.Length) resultat[k++] = d[j++];
    return resultat;
}
