var n = int.Parse(System.Console.ReadLine());

var mention = n switch
{
    >= 90 => "excellent",
    >= 50 => "passable",
    _ => "insuffisant",
};

System.Console.WriteLine(mention);
