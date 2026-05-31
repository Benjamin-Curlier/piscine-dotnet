# Module 34 — Interopérabilité (P/Invoke & code natif)

> **Module de lecture.** Contrairement aux autres modules, celui-ci n'a pas d'exercices
> auto-corrigés. L'interopérabilité appelle du **code natif spécifique au système** (noms de
> bibliothèques différents sous Windows / Linux / macOS) : ce n'est ni portable ni déterministe,
> donc impossible à corriger de façon fiable par la moulinette. Lis-le, puis **expérimente sur ta
> propre machine** avec les exemples ci-dessous.

.NET vit dans un monde **managé** : mémoire gérée par le GC, sûreté des types, portabilité. Mais
parfois il faut sortir de ce monde pour appeler une **bibliothèque native** (C/C++) : une API du
système d'exploitation, une librairie scientifique, un pilote… C'est le rôle de
l'**interopérabilité** (interop), dont le mécanisme principal est **P/Invoke** (*Platform Invoke*).

---

## 1. La frontière managé ↔ natif

| Côté managé (.NET) | Côté natif (C/C++) |
|---|---|
| mémoire gérée par le GC | mémoire gérée à la main (`malloc`/`free`) |
| types sûrs (`string`, `int[]`) | pointeurs bruts (`char*`, `int*`) |
| portable (un même IL partout) | compilé pour un OS/CPU précis |

Franchir cette frontière a un **coût** (marshalling + transition) et fait perdre les garanties du
managé. Règle d'or : **rester managé tant que possible**, n'utiliser l'interop que lorsque c'est
indispensable.

---

## 2. P/Invoke classique : `DllImport`

On déclare une méthode **externe** qui pointe vers une fonction d'une bibliothèque native. Le
runtime génère le « stub » de marshalling à l'exécution.

```csharp
using System.Runtime.InteropServices;

internal static partial class Natif
{
    // Windows : MessageBox de user32.dll
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(nint hWnd, string texte, string titre, uint type);
}
```

⚠️ `"user32.dll"` n'existe **que sous Windows**. Sous Linux, la même idée viserait `libc` :

```csharp
[DllImport("libc")]
public static extern int getpid();   // identifiant de processus (POSIX)
```

C'est précisément cette dépendance au système qui rend l'interop non portable « par défaut ».

---

## 3. P/Invoke moderne : `LibraryImport` (.NET 7+)

`[LibraryImport]` remplace `[DllImport]` par une approche **générée à la compilation** (source
generator) : le code de marshalling est produit et **visible**, plus rapide, et compatible AOT. La
méthode doit être `partial` et la classe `partial`.

```csharp
using System.Runtime.InteropServices;

internal static partial class Natif
{
    [LibraryImport("libc", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int puts(string s);
}
```

À privilégier dans le code neuf. (C'est aussi pourquoi ces appels ne passent pas dans une
moulinette « un seul fichier compilé à la volée » : le générateur de source n'y tourne pas.)

---

## 4. Le marshalling : traduire les types

Le **marshalling** convertit les données entre représentation managée et native.

- **Types blittables** (mêmes octets des deux côtés : `int`, `byte`, `double`, `nint`…) : copie
  directe, rapide.
- **Chaînes** : `string` managé ↔ `char*`/`wchar_t*` natif. On précise l'encodage
  (`StringMarshalling.Utf8`, `CharSet.Unicode`…).
- **Structures** : on fixe la disposition mémoire avec `[StructLayout]` pour qu'elle corresponde à
  la struct C.

```csharp
[StructLayout(LayoutKind.Sequential)]
struct Point { public int X; public int Y; }

[StructLayout(LayoutKind.Explicit)]
struct Union              // une "union" C : champs au même offset
{
    [FieldOffset(0)] public int Entier;
    [FieldOffset(0)] public float Flottant;
}
```

`nint`/`nuint` (entiers de taille pointeur) remplacent l'ancien `IntPtr` pour les handles et
adresses.

---

## 5. Gérer la mémoire et les handles

Le code natif alloue souvent de la mémoire ou des ressources que le GC **ignore**. Il faut donc :

- libérer explicitement (souvent via une fonction native dédiée) ;
- encapsuler les handles dans un **`SafeHandle`** : un wrapper `IDisposable` qui garantit la
  libération même en cas d'exception (cf. module 32) ;
- pour copier des octets à la frontière, utiliser la classe **`Marshal`**
  (`Marshal.AllocHGlobal`, `Marshal.Copy`, `Marshal.PtrToStructure<T>`…).

---

## 6. Code `unsafe` et pointeurs

Pour les scénarios de performance ou d'interop fine, C# autorise les **pointeurs** dans un contexte
`unsafe` (nécessite l'option de compilation `AllowUnsafeBlocks`). `fixed` « épingle » un tableau
pour que le GC ne le déplace pas pendant qu'on en tient l'adresse.

```csharp
unsafe
{
    int[] data = { 1, 2, 3 };
    fixed (int* p = data)
    {
        // p pointe sur data[0], utilisable par du code natif
    }
}
```

Aujourd'hui, **`Span<T>`** et **`Memory<T>`** couvrent la plupart des besoins de manipulation
mémoire **sans** `unsafe` : préfère-les quand c'est possible.

---

## 7. Quand (ne pas) faire de l'interop

**Oui** : API système absente de .NET, réutilisation d'une lib native existante, performance
extrême sur un noyau de calcul.

**Non / à éviter** : si une bibliothèque .NET équivalente existe, si la portabilité compte, si on
peut rester en managé. Chaque appel natif est une porte de sortie hors des garanties du runtime —
à n'ouvrir qu'à bon escient.

## Pour pratiquer (hors moulinette)

- Crée un petit projet console et appelle `getpid()` (Linux/macOS) ou `GetCurrentProcessId()` de
  `kernel32.dll` (Windows), puis compare avec `System.Environment.ProcessId`.
- Déclare une `[StructLayout(LayoutKind.Sequential)]` et observe `Marshal.SizeOf<T>()`.
- Réécris un `[DllImport]` en `[LibraryImport]` et regarde le code généré.

## Références externes

- [Native interoperability (doc Microsoft)](https://learn.microsoft.com/dotnet/standard/native-interop/)
- [P/Invoke source generation — LibraryImport](https://learn.microsoft.com/dotnet/standard/native-interop/pinvoke-source-generation)
- [Marshalling de types (doc Microsoft)](https://learn.microsoft.com/dotnet/standard/native-interop/type-marshalling)
