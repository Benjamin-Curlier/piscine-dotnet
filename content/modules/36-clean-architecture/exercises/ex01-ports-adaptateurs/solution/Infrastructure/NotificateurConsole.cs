using System;
using Domain;

namespace Infrastructure;

// Adaptateur du port INotificateur : envoie la notification sur la console. On pourrait le remplacer
// par un e-mail ou un fichier SANS toucher au domaine ni à l'application.
public sealed class NotificateurConsole : INotificateur
{
    public void Notifier(string message) => Console.WriteLine($"[notif] {message}");
}
