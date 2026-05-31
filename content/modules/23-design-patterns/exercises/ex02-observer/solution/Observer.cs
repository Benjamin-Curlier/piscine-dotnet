using System.Collections.Generic;

var sujet = new Sujet();
sujet.Abonner(new ObservateurA());
sujet.Abonner(new ObservateurB());

var n = int.Parse(System.Console.ReadLine());
for (var i = 0; i < n; i++)
{
    var message = System.Console.ReadLine();
    sujet.Diffuser(message);
}

interface IObservateur
{
    void Notifier(string message);
}

class ObservateurA : IObservateur
{
    public void Notifier(string message) => System.Console.WriteLine("[A] " + message);
}

class ObservateurB : IObservateur
{
    public void Notifier(string message) => System.Console.WriteLine("[B] " + message);
}

class Sujet
{
    private readonly List<IObservateur> _observateurs = new List<IObservateur>();

    public void Abonner(IObservateur observateur) => _observateurs.Add(observateur);

    public void Diffuser(string message)
    {
        foreach (var observateur in _observateurs)
        {
            observateur.Notifier(message);
        }
    }
}
