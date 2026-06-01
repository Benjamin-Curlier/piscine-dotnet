using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Lis un seuil, puis N, puis N lignes "nom prix". Affiche, triés par nom, les produits
// dont le prix est >= seuil.

var seuil = int.Parse(System.Console.ReadLine());
var n = int.Parse(System.Console.ReadLine());

// TODO : insère les produits, puis db.Produits.Where(x => x.Prix >= seuil).OrderBy(x => x.Nom).

// TODO : classe Produit { Id, Nom, Prix } et classe Catalogue : DbContext.
