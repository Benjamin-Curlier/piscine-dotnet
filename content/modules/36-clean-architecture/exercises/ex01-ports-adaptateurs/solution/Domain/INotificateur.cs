namespace Domain;

// Second port : « prévenir quelqu'un » d'un événement. Le domaine dit CE dont il a besoin ;
// l'infrastructure décidera COMMENT (console, e-mail, fichier…).
public interface INotificateur
{
    void Notifier(string message);
}
