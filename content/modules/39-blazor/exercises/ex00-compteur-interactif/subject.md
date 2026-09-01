# ex00 — Compteur interactif

Complète `Counter.razor` :

- active `InteractiveServer` ;
- affiche `Valeur : @_count` ;
- lie un `<input type="number">` au pas `_step` avec `@bind` ;
- le bouton appelle `Increment` ;
- `_count` commence à 0, `_step` à 1, et `Increment` ajoute le pas.

La moulinette inspecte le composant. Lance-le ensuite dans une Blazor Web App pour vérifier focus,
clavier et rendu réel.
