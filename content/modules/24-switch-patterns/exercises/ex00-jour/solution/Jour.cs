var n = int.Parse(System.Console.ReadLine());

var nom = n switch
{
    1 => "lundi",
    2 => "mardi",
    3 => "mercredi",
    4 => "jeudi",
    5 => "vendredi",
    6 => "samedi",
    7 => "dimanche",
    _ => "inconnu",
};

System.Console.WriteLine(nom);
