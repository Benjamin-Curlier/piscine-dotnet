using System.Collections.Generic;

// Lis des commandes ligne par ligne jusqu'à la fin de l'entrée :
//   "add N"  → ajoute N        "sub N" → soustrait N        "undo" → annule la dernière
// Modélise chaque opération comme un objet commande (Executer / Annuler) et garde une
// pile d'historique pour le undo. Affiche la valeur finale (départ : 0).

// TODO : lis les lignes, crée/exécute/empile les commandes, gère undo, affiche la valeur.

// TODO : classe Calculatrice, interface ICommande, classes Ajouter et Soustraire.
