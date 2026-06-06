# ex03-mutation — Des tests qui attrapent les bugs

## Objectif

Un test qui ne casse jamais ne sert à rien. Dans cet exercice, tu ne codes **pas** une
implémentation : tu écris les **tests** qui vérifient une implémentation donnée.

L'implémentation est cachée — tu vois seulement le contrat (ci-dessous). Le moteur va
compiler tes tests contre la version **correcte** (ils doivent tous passer), puis contre des
versions **boguées** appelées **mutants**. Chaque mutant contient un petit changement
intentionnel. Un bon test doit **échouer** sur le mutant : on dit qu'il le « tue ». Un mutant
qui survit révèle un cas que tu n'as pas vérifié.

## Le contrat de `Compte`

```csharp
public class Compte
{
    public int Solde { get; private set; } = 100;

    public bool Retirer(int montant) { ... }
}
```

- `Solde` commence à **100**.
- `Retirer(montant)` retourne `true` **et** décrémente `Solde` du montant retiré, si
  `montant <= Solde`.
- Sinon, il retourne `false` et laisse `Solde` **inchangé**.

## Livrable

- `CompteTests.cs` — ta suite de tests xUnit.

## Ce que le moteur vérifie

1. **Tous tes tests passent** sur l'implémentation correcte.
2. **Au moins un de tes tests échoue** sur chaque mutant.

## Cas à couvrir

Pour tuer tous les mutants, pense à :

- **La borne exacte** : `Retirer(100)` doit réussir (`montant == Solde`). Un mutant remplace
  `<=` par `<` et manquerait ce cas.
- **L'effet de bord sur le solde** : après `Retirer(40)`, `Solde` vaut-il bien `60` ? Un
  mutant ignore le débit (`-= 0`) — seul un `Assert.Equal` sur le solde le révèle.
- **Le cas de refus** : `Retirer(101)` doit retourner `false` et ne pas modifier le solde.

## Indices

- Utilise le patron **Arrange-Act-Assert** : construis un `Compte`, appelle `Retirer`, puis
  fais un `Assert.True` / `Assert.False` **et** un `Assert.Equal` sur `Solde` quand c'est
  pertinent.
- Nomme chaque test selon la convention `Methode_Condition_ResultatAttendu`, par exemple
  `Retirer_MontantEgalAuSolde_Reussit`.
- Un seul test bien ciblé suffit à tuer un mutant — mais plusieurs cas rendent ta suite plus
  robuste.
