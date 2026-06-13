using System;

// Lis N (première ligne), puis N lignes « a op b » (séparées par des espaces ; op ∈ + - * /).
// Pour chaque ligne i (de 1 à N), affiche soit « i: <résultat> », soit un message d'erreur :
//   - « i: erreur de format »   si un nombre est invalide (FormatException)
//   - « i: division par zero »  si division par 0 (DivideByZeroException)
//   - « i: operateur inconnu »  si l'opérateur n'est pas + - * /
// Une erreur sur une ligne ne doit PAS interrompre le traitement des suivantes.

int n = int.Parse(System.Console.ReadLine());
for (int i = 1; i <= n; i++)
{
    // TODO : lis la ligne, tente le calcul dans un try, attrape chaque type d'exception.
}
