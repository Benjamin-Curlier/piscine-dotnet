using System;

// Ligne 1 : un texte. Ligne 2 : une liste de décorations séparées par des virgules, parmi :
//   maj      -> met en MAJUSCULES (ToUpperInvariant)
//   crochets -> entoure de [ ]
//   etoiles  -> entoure de * *
// Applique les décorations DANS L'ORDRE (chacune enveloppe la précédente — pattern Decorator),
// puis affiche le rendu final. L'ORDRE change le résultat.
// Exemple : "bonjour" + "maj,crochets" -> "[BONJOUR]" ; "bonjour" + "crochets,maj" -> "[BONJOUR]"
//           (ici identiques) ; mais "salut" + "crochets,maj" -> "[SALUT]".

string texte = System.Console.ReadLine();
string decorations = System.Console.ReadLine();

// TODO : définis une interface ITexte { string Rendu(); }, une classe de base TexteBrut,
// et un décorateur par option (chacun enveloppe un ITexte). Compose-les dans une boucle.
