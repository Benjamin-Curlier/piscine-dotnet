using System.Collections.Generic;

var salaireFixe = int.Parse(System.Console.ReadLine());
var salaireBase = int.Parse(System.Console.ReadLine());
var commission = int.Parse(System.Console.ReadLine());

var employes = new List<Employe>
{
    new EmployeFixe(salaireFixe),
    new Commercial(salaireBase, commission),
};

var total = 0;
foreach (var employe in employes)
{
    total += employe.SalaireMensuel();
}

System.Console.WriteLine(total);

abstract class Employe
{
    public abstract int SalaireMensuel();
}

class EmployeFixe : Employe
{
    private readonly int _salaire;
    public EmployeFixe(int salaire) => _salaire = salaire;

    public override int SalaireMensuel() => _salaire;
}

class Commercial : Employe
{
    private readonly int _salaireBase;
    private readonly int _commission;

    public Commercial(int salaireBase, int commission)
    {
        _salaireBase = salaireBase;
        _commission = commission;
    }

    public override int SalaireMensuel() => _salaireBase + _commission;
}
