using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Lis N puis N lignes "auteur titre". Un auteur peut avoir plusieurs livres (relation 1-N).
// Affiche, par auteur trié, ses titres triés : "auteur: titre1, titre2".

var n = int.Parse(System.Console.ReadLine());

// TODO : réutilise un auteur déjà créé (dictionnaire nom -> Auteur), ajoute-lui ses Livres,
//        puis db.Auteurs.Include(a => a.Livres).OrderBy(a => a.Nom) et trie les titres.

// TODO : classes Auteur { Id, Nom, List<Livre> Livres }, Livre { Id, Titre, AuteurId, Auteur },
//        et Bibliotheque : DbContext { DbSet<Auteur>, DbSet<Livre> }.
