// Version refactorée : les conditions imbriquées deviennent des clauses-gardes (retours anticipés).
var age = int.Parse(System.Console.ReadLine());
var solde = int.Parse(System.Console.ReadLine());

System.Console.WriteLine(EstEligible(age, solde) ? "OUI" : "NON");

static bool EstEligible(int age, int solde)
{
    if (age < 18) { return false; }
    if (solde < 0) { return false; }
    if (solde > 100000) { return false; }
    return true;
}
