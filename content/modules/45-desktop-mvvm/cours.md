# Module 45 — Applications lourdes réactives

WPF, Avalonia, WinUI et les autres frameworks XAML diffèrent dans leurs API, mais partagent les
mêmes contraintes : état observable, thread UI unique, commandes utilisateur et opérations lentes
hors du thread d'affichage.

## 1. MVVM comme frontière {#mvvm}

La vue décrit l'affichage et les liaisons. Le ViewModel expose un état et des commandes testables. Le
modèle porte le métier. Le ViewModel ne devrait pas connaître un contrôle graphique concret ni ouvrir
directement une connexion SQL ou NATS : il dépend de ports injectés.

`INotifyPropertyChanged` informe la vue qu'une propriété calculée ou modifiée doit être relue. Un
toolkit MVVM peut générer le code répétitif, mais il ne décide pas où placer les responsabilités.

## 2. Le thread UI {#dispatcher}

Le thread UI traite les entrées et le rendu. Un calcul bloquant, `.Wait()` ou `.Result` fige donc la
fenêtre et peut créer un interblocage. Une opération I/O reste asynchrone ; un calcul CPU long peut
être déplacé vers un worker. La mise à jour finale repasse par l'abstraction de dispatcher du
framework.

Injecter `IUiDispatcher` rend cette frontière testable sans lancer de fenêtre. Le ViewModel exprime
« publie cet état sur l'UI », l'adaptateur appelle ensuite `Dispatcher.InvokeAsync` ou l'équivalent.

## 3. Annulation et résultats obsolètes {#latest-wins}

Une recherche déclenchée à chaque saisie illustre un piège classique : la première requête peut finir
après la seconde et écraser le résultat récent. Il faut annuler l'ancienne opération **et** vérifier
un numéro de révision avant de publier. L'annulation seule n'est pas une preuve : le serveur peut avoir
terminé juste avant de recevoir le signal.

Un ViewModel expose aussi explicitement `IsBusy`, l'erreur présentable et la possibilité de relancer.
Les exceptions techniques sont journalisées avec corrélation ; l'interface affiche un message utile.

## 4. Cycle de vie du poste {#cycle-vie}

À la fermeture, annule les travaux, arrête les abonnements, vide si nécessaire une file locale et
libère les ressources. Le poste peut perdre le réseau : distingue état local, état confirmé par le
serveur et commande en attente de synchronisation.

### Atelier réel

Branche les ViewModels sur une petite fenêtre Avalonia ou WPF. Simule 500 ms de latence, lance trois
recherches rapidement, redimensionne la fenêtre et vérifie que seul le dernier résultat apparaît sans
gel de l'UI.

Référence : [Modèle de threading WPF et dispatcher](https://learn.microsoft.com/dotnet/desktop/wpf/advanced/threading-model).
