// Lis un nom. Crée une Connexion, puis appelle Dispose() DEUX fois.
// Dispose() doit être idempotent : le second appel ne réaffiche pas "ferme".

var nom = System.Console.ReadLine();

// TODO : new Connexion(nom) ; .Dispose() ; .Dispose() ; puis la classe avec un drapeau _ferme.
