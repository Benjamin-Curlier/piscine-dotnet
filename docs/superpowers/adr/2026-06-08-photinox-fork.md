# ADR — Migration vers le fork PhotinoX.Blazor 4.2.0

> Décision : 2026-06-08. Statut : **ACTÉE**. Portée : hôte de bureau `Piscine.Desktop` uniquement.

## Contexte

`Piscine.Desktop` reposait sur `Photino.Blazor 3.2.0` (Photino.NET v3), qui :
- tire `Microsoft.AspNetCore.Components.WebView` en **8.0.x** → épingle manuelle `10.0.8` pour éviter
  un downgrade NU1605 sous `TreatWarningsAsErrors` ;
- cible **webkit2gtk-4.0** (soup2) sous Linux → runner figé `ubuntu-22.04`, prérequis `libwebkit2gtk-4.0`.

## Décision

Migrer vers **`PhotinoX.Blazor 4.2.0`** (fork maintenu d'`ivanvoyager`, Apache-2.0,
<https://github.com/ivanvoyager/PhotinoX.Blazor>), qui **cible nativement net8/9/10** et aligne WebView
sur **10.0.x**.

## Conséquences

- **+** Suppression de l'épingle WebView et du contournement NU1605.
- **+** Dépendance net10-native, activement maintenue (vs Photino 3.x).
- **~** Linux passe à **webkit2gtk-4.1** (soup3) : runner `ubuntu-24.04`, prérequis zip `libwebkit2gtk-4.1`,
  bundling AppImage offline en 4.1. Aligné sur les distros récentes (la 4.0 est en fin de vie upstream).
- **~** Libs natives renommées : `PhotinoX.Native.{dll,so}` (Windows garde `WebView2Loader.dll`) →
  assertions CI et docs mises à jour.
- **=** **Aucun** changement d'API : namespace `Photino.Blazor` et `PhotinoBlazorAppBuilder` conservés
  par le fork → code applicatif inchangé. Seul ajustement : `ShowMessage(text)` est annoté non-nullable
  → fallback sur `ExceptionObject.ToString()` (1 ligne, `Program.cs`).

## Alternative écartée

`Photino.Blazor 4.0.13` (upstream) : également net-multi-cible, mais PhotinoX est en avance de version
(4.2.0) et net10-natif ; le choix du fork est assumé pour suivre la maintenance active.
