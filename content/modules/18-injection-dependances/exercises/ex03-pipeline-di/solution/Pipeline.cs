using System;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<IFormateur, FormateurMajuscule>();
services.AddTransient<Rapport>();
using var provider = services.BuildServiceProvider();

var rapport = provider.GetRequiredService<Rapport>();
string ligne;
while ((ligne = System.Console.ReadLine()) is not null && ligne.Length > 0)
{
    System.Console.WriteLine(rapport.Produire(ligne));
}

interface IFormateur
{
    string Formater(string texte);
}

sealed class FormateurMajuscule : IFormateur
{
    public string Formater(string texte) => texte.ToUpperInvariant();
}

sealed class Rapport
{
    private readonly IFormateur _formateur;

    public Rapport(IFormateur formateur) => _formateur = formateur;

    public string Produire(string texte) => $"[{_formateur.Formater(texte)}]";
}
