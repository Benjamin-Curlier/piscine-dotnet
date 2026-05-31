// Version refactorée : les nombres magiques sont devenus des constantes nommées.
const int SeuilRemise = 100;
const int TauxRemisePourcent = 10;
const int TauxTvaPourcent = 20;

var prix = int.Parse(System.Console.ReadLine());

var remise = prix > SeuilRemise ? prix * TauxRemisePourcent / 100 : 0;
var net = prix - remise;
var ttc = net + net * TauxTvaPourcent / 100;

System.Console.WriteLine(ttc);
