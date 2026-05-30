# Module 00 — Mise en place & premiers pas Git

Bienvenue dans la **Piscine .NET** ! Ce premier module a deux buts : vérifier que ton
environnement fonctionne, et te faire faire ton **premier rendu en git**. Pas de pression de
temps : avance à ton rythme.

## 1. Lancer la piscine

Après avoir dézippé la piscine (voir le guide de mise en œuvre) :

```bash
piscine init        # prépare ton espace de travail + le dépôt de rendu (une seule fois)
piscine status      # affiche la bannière et ton état
piscine list        # liste les modules et exercices disponibles
```

`piscine init` crée :

- un **workspace** : le dossier où tu écris ton code ;
- un **dépôt git local** qui joue le rôle du « GitLab » de l'équipe (`origin`) ;
- un déclencheur qui lance la correction automatique à chaque envoi (`git push`).

## 2. La boucle de travail

```bash
piscine start ex00-hello   # copie le squelette de l'exercice dans ton workspace
# ... tu codes ...
piscine check ex00-hello   # correction instantanée (autant de fois que tu veux, ne compte pas)
```

Quand tu es satisfait·e, tu **rends officiellement** avec git :

```bash
git add .
git commit -m "ex00-hello"
git push origin main       # déclenche la moulinette et enregistre ta progression
```

- `piscine check` = brouillon : feedback immédiat, **ne compte pas** comme rendu.
- `git push origin main` = **rendu officiel**, comme dans la vraie vie.

> La correction est **éducative** : on t'explique *ce qui était attendu* vs *ce que tu as obtenu*.
> Pas de note chiffrée. Statuts : **Réussi**, **À revoir**, **Non corrigé**.

## 3. Hello world en C# {#hello-world}

Un programme C# minimal peut s'écrire en une ligne (« top-level statements ») :

```csharp
System.Console.WriteLine("Bonjour !");
```

- `System.Console.WriteLine(...)` affiche un texte **suivi d'un retour à la ligne**.
- `System.Console.Write(...)` affiche **sans** retour à la ligne.
- Les guillemets `"..."` délimitent une chaîne de caractères. Respecte **exactement** la casse,
  la ponctuation et les espaces : la moulinette compare le texte au caractère près.

## 4. Lire l'entrée standard {#lire-l-entree}

Pour lire une ligne tapée par l'utilisateur :

```csharp
var nom = System.Console.ReadLine();        // lit une ligne (sans le retour à la ligne)
System.Console.WriteLine($"Bonjour, {nom}!"); // $"..." insère la valeur de nom
```

Le `$` devant la chaîne active l'**interpolation** : `{nom}` est remplacé par sa valeur.

## Pour aller plus loin

- Microsoft Learn — *Premiers pas en C#* : <https://learn.microsoft.com/fr-fr/dotnet/csharp/>
- freeCodeCamp (FR) — bases de la programmation.
- Documentation officielle .NET : <https://learn.microsoft.com/fr-fr/dotnet/>

À toi de jouer : `piscine start ex00-hello`.
