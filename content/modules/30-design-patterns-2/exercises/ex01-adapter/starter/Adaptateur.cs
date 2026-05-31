// 1re ligne : un annuaire legacy au format "nom:age,nom:age,...".
// 2e ligne : un nom à rechercher.
// Le client n'utilise que l'interface IAnnuaire ; écris un adaptateur qui s'appuie
// sur la classe legacy pour répondre à Age(nom). Affiche l'âge, ou -1 si absent.

var donnees = System.Console.ReadLine();
var nom = System.Console.ReadLine();

// TODO : instancie l'adaptateur autour de l'API legacy et affiche annuaire.Age(nom).

// TODO : interface IAnnuaire, classe AnnuaireLegacy, classe AnnuaireAdapter.
