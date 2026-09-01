#:property PublishAot=false
#:package Microsoft.EntityFrameworkCore@10.0.11
#:package Microsoft.EntityFrameworkCore.Sqlite@10.0.11

using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Lis N puis N lignes "categorie nom". Affiche, par catégorie triée, le nombre
// d'articles : "categorie: nombre".

var n = int.Parse(System.Console.ReadLine());

// TODO : insère chaque Article, puis construis la requête en quatre étapes :
// source -> groupes par catégorie -> projection clé/compte -> tri par clé.

// TODO : classe Article { Id, Categorie, Nom } et classe Catalogue : DbContext.
