using System;

// Lis une expression arithmétique en notation polonaise PRÉFIXE, ex. "+ 3 * 4 2".
// Modélise un arbre comme une union RÉCURSIVE : Nombre(valeur) ou Operation(symbole,
// gauche, droite). Lis récursivement, puis évalue par un switch récursif. Affiche le résultat.

var tokens = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// TODO : lis l'arbre récursivement (si token + ou *, lis deux sous-arbres ; sinon nombre).
// TODO : évalue récursivement et affiche.

// TODO : abstract record Expr; sealed record Nombre(int Valeur) : Expr; sealed record Operation(string Symbole, Expr Gauche, Expr Droite) : Expr;
