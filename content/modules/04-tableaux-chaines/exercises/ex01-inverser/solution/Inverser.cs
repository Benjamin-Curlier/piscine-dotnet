var s = System.Console.ReadLine();
var lettres = s.ToCharArray();
System.Array.Reverse(lettres);
System.Console.WriteLine(new string(lettres));
