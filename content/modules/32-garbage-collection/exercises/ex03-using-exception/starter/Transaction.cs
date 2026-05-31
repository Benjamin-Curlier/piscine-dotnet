// Lis un nom. Dans un try : ouvre une Transaction en using, affiche "debut", puis
// lève une exception. Le catch affiche "erreur attrapee". Vérifie que "ferme" est
// affiché AVANT "erreur attrapee" (le using libère pendant le déroulement de la pile).

var nom = System.Console.ReadLine();

// TODO : try { using var t = new Transaction(nom); "debut" ; throw ... } catch { "erreur attrapee" }
