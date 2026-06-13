using System.Linq;

var mot1 = System.Console.ReadLine().ToLower();
var mot2 = System.Console.ReadLine().ToLower();

var tri1 = mot1.OrderBy(c => c).ToArray();
var tri2 = mot2.OrderBy(c => c).ToArray();

var sontAnagrammes = tri1.SequenceEqual(tri2) && mot1 != mot2;
System.Console.WriteLine(sontAnagrammes ? "oui" : "non");
