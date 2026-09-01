using System;

// La machine commence EnAttente. Lis N puis N commandes : demarrer, progres P, terminer X.
// Après une transition acceptée, affiche le nouvel état. Sinon affiche "transition refusee".

var n = int.Parse(System.Console.ReadLine());
Etat etat = new EnAttente();

// TODO : pour chaque commande, calcule la transition selon l'état courant et la commande.
//        Si elle est refusée, garde l'ancien état. Sinon, affiche le nouvel état.

// TODO : abstract record Etat; sealed record EnAttente : Etat; sealed record EnCours(int Pourcent) : Etat; sealed record Termine(string Resultat) : Etat;
