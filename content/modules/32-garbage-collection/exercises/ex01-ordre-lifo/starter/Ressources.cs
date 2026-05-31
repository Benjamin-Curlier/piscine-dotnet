// Lis trois noms séparés par des espaces. Ouvre trois ressources avec des
// using declarations (using var ...), affiche "travail", et observe l'ordre de
// libération : INVERSE de la déclaration (la dernière ouverte est la première fermée).

var noms = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// TODO : using var a = new Ressource(noms[0]); ... ; "travail" ; puis la classe Ressource.
