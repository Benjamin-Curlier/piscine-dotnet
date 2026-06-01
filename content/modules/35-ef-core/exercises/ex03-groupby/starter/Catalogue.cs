using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Lis N puis N lignes "categorie nom". Affiche, par catégorie triée, le nombre
// d'articles : "categorie: nombre".

var n = int.Parse(System.Console.ReadLine());

// TODO : insère chaque Article { Categorie, Nom }, puis
//        db.Articles.GroupBy(a => a.Categorie).Select(g => new { g.Key, N = g.Count() }).OrderBy(x => x.Key).

// TODO : classe Article { Id, Categorie, Nom } et classe Catalogue : DbContext.
