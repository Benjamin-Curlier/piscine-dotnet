using System.Threading.Tasks;

var n = int.Parse(System.Console.ReadLine());

var verrou = new object();
int compteur = 0;
System.Threading.Tasks.Parallel.For(0, n, _ =>
{
    lock (verrou)
    {
        compteur++;
    }
});

System.Console.WriteLine(compteur);
