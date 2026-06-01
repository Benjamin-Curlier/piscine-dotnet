using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Lis N puis N lignes "nom prix". Insère-les, puis affiche tous les produits triés
// par nom, une ligne "nom prix" par produit. Le OrderBy est essentiel : il rend la
// sortie déterministe.

var n = int.Parse(System.Console.ReadLine());

// TODO : lis les lignes, insère chaque Produit { Nom, Prix }, puis
//        foreach (var p in db.Produits.OrderBy(x => x.Nom)) ...

// TODO : classe Produit { Id, Nom, Prix } et classe Catalogue : DbContext.
