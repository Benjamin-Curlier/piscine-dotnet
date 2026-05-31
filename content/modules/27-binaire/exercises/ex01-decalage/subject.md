# ex01-decalage — Décalages

## Objectif

Lis un entier **n** puis un entier **k** (un par ligne). Affiche sur **2 lignes** :

1. `n << k` (décalage à gauche de `k` bits)
2. `n >> k` (décalage à droite de `k` bits)

Exemple : `1` puis `4` → `16`, `0`.

## Livrable

- `Decalage.cs`

## Indices

- Décaler à **gauche** de 1 bit revient à **multiplier par 2** ; décaler à **droite** de 1 bit
  revient à **diviser par 2** (division entière).
- `1 << 4` déplace le bit de 4 positions : `10000` en binaire, soit `16`. `1 >> 4` fait
  « tomber » le seul bit : il reste `0`.
- Lis chaque entier avec `int.Parse(System.Console.ReadLine())`.
