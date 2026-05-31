# ex02-channel — Producteur / consommateur avec Channel

## Objectif

Lis un entier **N**, puis **N** entiers. Un **producteur** écrit les N entiers dans un
`Channel<int>` puis le **complète** ; un **consommateur** les lit **dans l'ordre** (le canal est
FIFO) et affiche le **double** de chacun (un par ligne).

Exemple : `3` puis `2`, `3`, `4` → `4`, `6`, `8`.

## Livrable

- `Channel.cs`

## Indices

- Crée le canal : `var channel = System.Threading.Channels.Channel.CreateUnbounded<int>();`.
- **Producteur** : lis N entiers et, pour chacun, `await channel.Writer.WriteAsync(x);`. Quand
  tout est écrit, signale la fin : `channel.Writer.Complete();`.
- **Consommateur** : `await foreach (var x in channel.Reader.ReadAllAsync()) { ... }` lit chaque
  valeur dès qu'elle arrive et s'arrête à la complétion. Affiche `x * 2`.
- Le canal préserve l'ordre d'écriture (FIFO) : la sortie est donc **déterministe**.
- `using System.Threading.Channels;` (pour `Channel`) et `using System.Threading.Tasks;` sont
  nécessaires. `await` fonctionne directement au niveau des instructions principales.
