# ex01-salutation — Salutation

## Objectif

Lis un **prénom** sur l'entrée standard, puis affiche **exactement** :

```
Bonjour, <prénom>!
```

Par exemple, si l'entrée est `Alice`, le programme affiche `Bonjour, Alice!` (avec un retour
à la ligne final).

## Livrable

- `Salutation.cs`

## Indices

- `var nom = System.Console.ReadLine();` lit une ligne (sans le retour à la ligne).
- `System.Console.WriteLine($"Bonjour, {nom}!");` insère le prénom dans le message.

## Pour rendre

```bash
piscine check ex01-salutation
git add . && git commit -m "ex01-salutation" && git push origin main
```
