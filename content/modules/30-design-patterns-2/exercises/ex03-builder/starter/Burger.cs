using System.Collections.Generic;

// Lis une ligne d'ingrédients extra séparés par des espaces.
// Avec un builder fluide (chaque Avec(...) renvoie le builder), pars de [pain, steak]
// et ajoute les extras. Affiche "Burger : ... (N ingrédients)".

var extras = System.Console.ReadLine().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

// TODO : utilise BurgerBuilder pour ajouter chaque extra, puis affiche Construire().

// TODO : classe BurgerBuilder (liste interne, méthode Avec qui renvoie this, méthode Construire).
