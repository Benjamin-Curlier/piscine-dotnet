using System.Collections.Generic;

var n = int.Parse(System.Console.ReadLine());
var pile = new MaPile();

for (var i = 0; i < n; i++)
    pile.Empiler(int.Parse(System.Console.ReadLine()));

while (!pile.EstVide())
    System.Console.WriteLine(pile.Depiler());

class MaPile
{
    private readonly List<int> _elements = new List<int>();

    public void Empiler(int valeur) => _elements.Add(valeur);

    public int Depiler()
    {
        var dernier = _elements[_elements.Count - 1];
        _elements.RemoveAt(_elements.Count - 1);
        return dernier;
    }

    public bool EstVide() => _elements.Count == 0;
}
