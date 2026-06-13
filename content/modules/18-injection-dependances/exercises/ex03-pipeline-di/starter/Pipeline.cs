using System;
using Microsoft.Extensions.DependencyInjection;

// Construis un conteneur DI qui enregistre :
//   - IFormateur -> FormateurMajuscule (met le texte en MAJUSCULES, via ToUpperInvariant)
//   - Rapport (qui dépend de IFormateur par injection de constructeur ; Produire(t) renvoie "[<t formaté>]")
// Résous un Rapport, puis pour chaque ligne lue (jusqu'à la fin de l'entrée) affiche rapport.Produire(ligne).

var services = new ServiceCollection();
// TODO : services.AddSingleton<IFormateur, FormateurMajuscule>(); services.AddTransient<Rapport>();
// TODO : var provider = services.BuildServiceProvider(); var rapport = provider.GetRequiredService<Rapport>();
// TODO : boucle de lecture + affichage.

// TODO : interface IFormateur { string Formater(string texte); }
// TODO : class FormateurMajuscule : IFormateur { ... }
// TODO : class Rapport { public Rapport(IFormateur f) {...}  public string Produire(string t) => $"[{...}]"; }
