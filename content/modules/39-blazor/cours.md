# Module 39 — Blazor (interfaces web en C#)

> **Module de lecture.** Pas d'exercices auto-corrigés : une appli Blazor est un **site web**
> (serveur HTTP + rendu HTML/DOM dans un navigateur). Sa sortie n'est pas un `stdout` comparable au
> caractère près, et la corriger demanderait un navigateur *headless* — ni déterministe ni portable
> pour la moulinette console. Lis ce module, puis **construis et lance une appli sur ta machine**.

Jusqu'ici, tes programmes parlaient à la **console**. Avec **Blazor**, tu écris des **interfaces web**
(des pages, des boutons, des formulaires) **en C#** au lieu de JavaScript. Tu réutilises tout ce que
tu sais : types, DI, async, LINQ — mais le résultat s'affiche dans un navigateur.

---

## 1. Le composant, brique de base {#composant}

Une UI Blazor est un arbre de **composants**. Un composant = un fichier **`.razor`** qui mélange du
**balisage HTML** et du **C#** (dans un bloc `@code`). Il est réutilisable et paramétrable.

```razor
@rendermode InteractiveServer

<h1>Compteur</h1>
<p>Valeur : @_compte</p>
<button @onclick="Incrementer">+1</button>

@code {
    private int _compte;

    private void Incrementer() => _compte++;
}
```

- `@_compte` **interpole** une variable C# dans le HTML.
- `@onclick="Incrementer"` lie un **événement DOM** à une méthode C#.
- Après le clic, Blazor **re-rend** le composant et met à jour le DOM (différentiel).
- `@rendermode InteractiveServer` **active l'interactivité** : sans lui, en Blazor Web App
  (.NET 8+), le composant est rendu en **Static SSR** (HTML statique) et le bouton `@onclick` est
  **inerte** — le clic ne déclenche rien (voir le §5 sur les modèles de rendu).

## 2. Paramètres & liaison de données {#parametres}

Un composant reçoit des **paramètres** (comme des arguments), marqués `[Parameter]` :

```razor
<!-- Salutation.razor -->
<p>Bonjour, @Nom !</p>

@code {
    [Parameter] public string Nom { get; set; } = "monde";
}
```

```razor
<Salutation Nom="Alice" />
```

La **liaison bidirectionnelle** `@bind` synchronise un champ de formulaire avec une variable :

```razor
<input @bind="_pseudo" />
<p>Tu as tapé : @_pseudo</p>

@code { private string _pseudo = ""; }
```

Pour remonter un événement vers le parent, on expose un `EventCallback`.

## 3. Le cycle de vie d'un composant {#cycle-de-vie}

Blazor appelle des méthodes à des moments clés :

- **`OnInitialized` / `OnInitializedAsync`** : à la création (charger des données).
- **`OnParametersSet` / …Async** : quand les paramètres changent.
- **`OnAfterRender` / …Async** : après le rendu (interop JS, focus…).
- **`StateHasChanged()`** : signale à Blazor qu'il faut re-rendre (utile après un événement asynchrone
  externe, p. ex. un timer).

```razor
@code {
    private string[] _items = [];

    protected override async Task OnInitializedAsync()
        => _items = await Service.ChargerAsync();
}
```

## 4. Injection de dépendances dans un composant {#di}

Comme partout en .NET, un composant **demande ses dépendances** par injection :

```razor
@inject MonService Service

@code {
    protected override void OnInitialized() => _data = Service.Lire();
}
```

Les services sont enregistrés au démarrage (`builder.Services.AddScoped<MonService>()`), exactement
comme dans le module DI.

## 5. Les modèles de rendu (.NET 10) {#modeles-de-rendu}

Une **Blazor Web App** unifie plusieurs **modes de rendu**, choisis par page ou par composant :

- **Static SSR** : HTML rendu côté serveur, **sans interactivité** (rapide, idéal pour du contenu).
- **Interactive Server** : l'UI vit côté serveur ; les événements transitent par une connexion
  temps réel (SignalR). Léger à charger, nécessite la connexion.
- **Interactive WebAssembly** : le composant s'exécute **dans le navigateur** (.NET compilé en WASM) ;
  fonctionne hors-ligne après chargement.
- **Interactive Auto** : Server au premier chargement, puis bascule WebAssembly une fois téléchargé.

> ⚠️ **Static SSR est le mode par défaut.** En Blazor Web App (.NET 8+), un composant est rendu en
> HTML statique tant que tu n'as pas **choisi un mode interactif**. Sans interactivité, `@onclick`,
> `@bind` et les gestionnaires d'événements sont **inertes** : la page s'affiche, mais rien ne réagit.
> On active l'interactivité en plaçant une directive `@rendermode` en tête du composant
> (`@rendermode InteractiveServer` ou `InteractiveWebAssembly`), ou sur la balise à l'usage
> (`<Compteur @rendermode="InteractiveServer" />`). C'est l'oubli n° 1 des débutants.

S'y ajoutent le **streaming rendering** (envoyer le HTML au fur et à mesure) et la **navigation
améliorée**. Le bon mode dépend du compromis latence / interactivité / charge serveur.

## 6. Pourquoi pas d'auto-notation ? {#non-determinisme}

- **C'est un serveur web** : il faut héberger l'appli (Kestrel) et un **navigateur** pour voir le DOM.
- **Sortie = DOM**, pas un `stdout` : la comparer demande un harnais headless (type bUnit / Playwright),
  hors du modèle « console déterministe » de la moulinette.
- Le rendu `.razor` passe par un **générateur de source** au build, que le correcteur in-process
  n'exécute pas.

Pour ces raisons, Blazor reste un module **guidé** : on évalue en regardant l'appli tourner dans le
navigateur. *(Un harnais web dédié pourra venir plus tard, hors moulinette console.)*

## 7. À pratiquer (sur ta machine) {#pratique}

1. Crée une **Blazor Web App** : `dotnet new blazor -o MonAppli`, puis `dotnet run`.
2. Ouvre la page **Counter** : comprends le `@onclick` et le re-rendu.
3. Crée un composant `Salutation.razor` avec un `[Parameter] Nom` ; utilise-le dans une page.
4. Ajoute un `<input @bind="..." />` et affiche la valeur en direct.
5. Injecte un petit service (`AddScoped`) qui fournit une liste, charge-la dans `OnInitializedAsync`,
   affiche-la avec une boucle `@foreach`.
6. (Avancé) Compare les modes **Interactive Server** et **WebAssembly** sur un même composant.

## Références externes

- Microsoft Learn — *Blazor* (parcours complet) et *ASP.NET Core Blazor fundamentals*.
- Documentation *Blazor render modes* (.NET 8+/10).
- *bUnit* (test de composants) pour aller plus loin sur la vérification.
