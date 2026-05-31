# Module 30 — Design patterns (suite)

Le module 23 a introduit trois patrons « comportementaux » (Strategy, Factory, Observer). On
poursuit ici avec cinq patrons parmi les plus utilisés au quotidien. Un **design pattern** n'est
pas du code à copier : c'est une *solution éprouvée* à un problème récurrent de conception, qu'on
adapte au contexte. Les connaître donne un vocabulaire commun (« mets un adaptateur », « fais-en
un singleton ») et évite de réinventer des structures fragiles.

On classe traditionnellement les patrons GoF en trois familles :
- **Créationnels** — comment créer des objets (Singleton, Builder, Factory…).
- **Structurels** — comment composer des objets (Adapter, Decorator…).
- **Comportementaux** — comment les objets collaborent (Command, Strategy, Observer…).

---

## 1. Singleton {#singleton}

**Problème** : garantir qu'une classe n'a **qu'une seule instance** et offrir un point d'accès
global à celle-ci (configuration, cache, compteur partagé…).

**Solution** : constructeur **privé** (personne ne peut faire `new`) + propriété statique qui
détient l'unique instance.

```csharp
sealed class Compteur
{
    private static Compteur? _instance;
    public static Compteur Instance => _instance ??= new Compteur();
    private Compteur() { }
    public int Valeur { get; private set; }
    public void Incrementer() => Valeur++;
}
```

Tout appel à `Compteur.Instance` renvoie le **même** objet : un état modifié ici est visible
partout. ⚠️ À utiliser avec parcimonie — un singleton est un état global déguisé, qui complique
les tests. En .NET moderne, on lui préfère souvent l'**injection de dépendances** avec une durée
de vie *singleton* (cf. module 18).

---

## 2. Adapter {#adapter}

**Problème** : on veut utiliser une classe existante, mais son interface ne correspond pas à ce
que le code attend (API legacy, bibliothèque tierce…).

**Solution** : un **adaptateur** implémente l'interface attendue et **traduit** les appels vers
la classe existante. Le client ne voit que l'interface ; l'adaptateur fait le pont.

```csharp
interface IAnnuaire { int Age(string nom); }

sealed class AnnuaireAdapter : IAnnuaire   // interface attendue par le client
{
    private readonly AnnuaireLegacy _legacy;            // l'API qu'on adapte
    public AnnuaireAdapter(AnnuaireLegacy legacy) => _legacy = legacy;
    public int Age(string nom) { /* parse _legacy.Tout() et renvoie l'âge */ }
}
```

C'est l'équivalent logiciel d'un **adaptateur de prise** : on ne change ni la prise murale ni
l'appareil, on intercale une pièce qui les rend compatibles.

---

## 3. Decorator {#decorator}

**Problème** : ajouter des responsabilités à un objet **dynamiquement**, sans faire exploser le
nombre de sous-classes (CaféAvecLait, CaféAvecLaitEtSucre, CaféAvecSucre…).

**Solution** : des **décorateurs** qui implémentent la même interface que l'objet et l'**enveloppent**.
Chaque décorateur délègue à l'objet enveloppé, puis ajoute sa contribution.

```csharp
interface IBoisson { string Description(); int Cout(); }

abstract class Decorateur : IBoisson
{
    protected readonly IBoisson Enveloppe;
    protected Decorateur(IBoisson enveloppe) => Enveloppe = enveloppe;
    public abstract string Description();
    public abstract int Cout();
}

sealed class Lait : Decorateur
{
    public Lait(IBoisson b) : base(b) { }
    public override string Description() => Enveloppe.Description() + ", lait";
    public override int Cout() => Enveloppe.Cout() + 1;
}
```

On empile alors les décorateurs comme des poupées russes : `new Sucre(new Lait(new Cafe()))`. Chaque
couche s'appuie sur la précédente. C'est la même idée que les `Stream` de .NET
(`GZipStream` enveloppant un `FileStream`…).

---

## 4. Builder {#builder}

**Problème** : construire un objet complexe (beaucoup d'options facultatives) sans un constructeur
à dix paramètres illisible.

**Solution** : un **builder** monte l'objet **pas à pas**. Les méthodes de configuration renvoient
le builder lui-même (`return this;`), ce qui permet de **chaîner** les appels (interface *fluide*).

```csharp
var burger = new BurgerBuilder()
    .Avec("fromage")
    .Avec("bacon")
    .Construire();
```

```csharp
sealed class BurgerBuilder
{
    private readonly List<string> _ingredients = new() { "pain", "steak" };
    public BurgerBuilder Avec(string ingredient) { _ingredients.Add(ingredient); return this; }
    public string Construire() => /* assemble la description finale */;
}
```

On retrouve ce style partout en .NET : `StringBuilder`, `HostBuilder`,
`WebApplication.CreateBuilder(...)`.

---

## 5. Command {#command}

**Problème** : on veut traiter des **actions** comme des données — les paramétrer, les mettre en
file, les journaliser, et surtout pouvoir les **annuler** (undo).

**Solution** : encapsuler chaque action dans un objet **commande** qui sait s'**exécuter** et se
**défaire**. Un historique (pile) des commandes exécutées permet le undo.

```csharp
interface ICommande
{
    void Executer(Calculatrice c);
    void Annuler(Calculatrice c);   // l'inverse exact d'Executer
}

sealed class Ajouter : ICommande
{
    private readonly int _n;
    public Ajouter(int n) => _n = n;
    public void Executer(Calculatrice c) => c.Valeur += _n;
    public void Annuler(Calculatrice c) => c.Valeur -= _n;
}
```

À l'exécution, on empile la commande ; sur `undo`, on **dépile** et on appelle `Annuler`. C'est le
mécanisme derrière le Ctrl+Z de n'importe quel éditeur.

---

## 6. En pratique {#pratique}

- Un pattern résout un problème **précis** : ne force pas un patron là où une simple méthode suffit.
- Beaucoup de patrons GoF sont déjà **intégrés** au framework .NET (Builder, Decorator via les
  `Stream`, Iterator via `IEnumerable`…). Sache les reconnaître autant que les écrire.
- `Singleton` et état global : préfère l'**injection de dépendances** dès qu'un projet grossit.

### Exercices du module

- **ex00-singleton** — instance unique partagée.
- **ex01-adapter** — rendre compatible une API existante.
- **ex02-decorator** — empiler des comportements.
- **ex03-builder** — construction fluide pas à pas.
- **ex04-command** *(bonus)* — actions réifiées & undo.

## Références externes

- [Refactoring.Guru — Design Patterns](https://refactoring.guru/design-patterns)
- [Design patterns in .NET (doc Microsoft Learn)](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles)
