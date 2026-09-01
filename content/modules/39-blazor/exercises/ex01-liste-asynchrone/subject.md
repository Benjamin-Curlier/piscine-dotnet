# ex01 — Liste asynchrone

Complète `Missions.razor` :

- injecte `IMissionService` sous le nom `Service` ;
- tant que `_loading` vaut vrai, affiche `Chargement…` ;
- sinon, affiche chaque mission de `_missions` dans un `<li>` ;
- surcharge `OnInitializedAsync`, appelle `await Service.GetAllAsync()` et affecte le résultat ;
- garantis `_loading = false` dans un bloc `finally`.

Le contrat `IMissionService` appartient à l'application hôte ; le composant est inspecté sans être
compilé isolément.
