using System;
using Domain;

namespace Infrastructure;

// Couche INFRASTRUCTURE : l'adaptateur concret du port INotificateur.
public sealed class NotificateurConsole : INotificateur
{
    // TODO : Notifier(message) écrit sur la console, exactement : [notif] <message>
}
