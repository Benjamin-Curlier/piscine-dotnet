# Module 38 — Silk.NET (fenêtrage & rendu graphique)

> **Module de lecture.** Pas d'exercices auto-corrigés : ouvrir une **fenêtre** et dessiner avec le
> **GPU** demande un affichage, des pilotes graphiques et des bibliothèques natives — c'est
> spécifique à la machine et **non déterministe** (impossible à corriger de façon fiable, surtout en
> CI *headless*). Lis ce module, puis **expérimente sur ta propre machine**.

Tu as surtout écrit des programmes console. Mais .NET sait aussi piloter le **matériel graphique**.
**Silk.NET** est la bibliothèque .NET de référence pour cela : des **bindings** haute performance vers
les grandes API natives — **OpenGL**, **Vulkan**, **Direct3D**, **OpenAL** (audio), **OpenCL**
(calcul) — plus le **fenêtrage** et les **entrées** (clavier/souris/manette).

---

## 1. Ce qu'est Silk.NET {#presentation}

- Une couche **« 1:1 »** au-dessus des API natives : les fonctions C sont exposées telles quelles en
  C#, sans surcouche qui cache le fonctionnement. Tu apprends donc **vraiment** OpenGL/Vulkan.
- **Haute performance** : usage de `Span<T>`, de pointeurs (`unsafe`) et d'appels directs, pensé pour
  le temps réel (jeux, visualisation, moteurs).
- **Multiplateforme** : Windows, Linux, macOS, et même navigateur/mobile selon les modules.
- Compatible **Native AOT** (démarrage rapide, pas de JIT).

Silk.NET est *bas niveau* : c'est puissant mais verbeux. Pour un vrai jeu, on s'appuie souvent sur un
moteur (MonoGame, Stride, Unity) ; Silk.NET sert quand on veut **maîtriser** la pile graphique.

---

## 2. Les paquets {#paquets}

On n'installe que ce dont on a besoin :

- **`Silk.NET.Windowing`** : créer une fenêtre et sa boucle de rendu (via GLFW ou SDL).
- **`Silk.NET.Input`** : clavier, souris, manettes.
- **`Silk.NET.OpenGL`** (ou `Silk.NET.Vulkan`, `Silk.NET.Direct3D11`…) : l'API de rendu.
- **`Silk.NET.Maths`** : vecteurs/matrices (utile pour la 3D).

```bash
dotnet add package Silk.NET.Windowing
dotnet add package Silk.NET.OpenGL
dotnet add package Silk.NET.Input
```

---

## 3. La fenêtre et la boucle de rendu {#boucle}

Une application graphique tourne en **boucle** : tant que la fenêtre est ouverte, on **met à jour**
l'état puis on **dessine** une image (*frame*), des dizaines de fois par seconde. Silk.NET expose
cela par des **événements** sur un `IWindow` :

- **`Load`** : initialisation unique (charger l'API GL, les ressources).
- **`Update`** (Δt) : faire avancer la logique (physique, entrées) — indépendant du dessin.
- **`Render`** (Δt) : dessiner la frame.
- **`Closing`** : libérer les ressources.

```csharp
using Silk.NET.Maths;
using Silk.NET.Windowing;

var options = WindowOptions.Default with
{
    Size = new Vector2D<int>(800, 600),
    Title = "Ma première fenêtre Silk.NET",
};

using var window = Window.Create(options);

window.Load += () => System.Console.WriteLine("Fenêtre prête.");
window.Update += dt => { /* logique : entrées, animation… */ };
window.Render += dt => { /* dessin de la frame */ };

window.Run(); // boucle bloquante jusqu'à fermeture
```

`Run()` **bloque** jusqu'à la fermeture de la fenêtre : c'est l'opposé du modèle console
« lire/écrire puis terminer », et l'une des raisons pour lesquelles ce module n'est pas auto-noté.

---

## 4. Dessiner avec OpenGL : effacer l'écran {#opengl}

Le « hello world » graphique consiste à **effacer** l'écran avec une couleur. On récupère l'API GL au
`Load`, puis on l'utilise au `Render` :

```csharp
using Silk.NET.OpenGL;

GL gl = null!;

window.Load += () => gl = window.CreateOpenGL();

window.Render += dt =>
{
    gl.ClearColor(0.1f, 0.2f, 0.4f, 1.0f); // bleu nuit
    gl.Clear(ClearBufferMask.ColorBufferBit);
    // dessins suivants : buffers, shaders, draw calls…
};
```

Aller plus loin (hors lecture) : **VBO/VAO** (les données des sommets), **shaders** GLSL compilés
(vertex + fragment), puis `gl.DrawArrays`/`DrawElements` pour tracer des triangles — la base de tout
rendu 3D.

---

## 5. Les entrées {#entrees}

Au `Load`, on ouvre un **contexte d'entrée** et on s'abonne aux périphériques :

```csharp
using Silk.NET.Input;

window.Load += () =>
{
    var input = window.CreateInput();
    foreach (var clavier in input.Keyboards)
    {
        clavier.KeyDown += (kb, touche, code) =>
        {
            if (touche == Key.Escape)
            {
                window.Close();
            }
        };
    }
};
```

---

## 6. Pourquoi pas d'auto-notation ? {#non-determinisme}

- **Affichage requis** : pas de fenêtre ni de GPU en environnement *headless* / CI.
- **Non déterministe** : la sortie est une **image** rendue par le pilote, variable selon le matériel
  et le pilote — pas un `stdout` comparable au caractère près.
- **Temps réel** : la boucle dépend du `Δt` et du taux de rafraîchissement.

Pour ces raisons, Silk.NET reste un module **guidé** : on évalue en regardant le résultat à l'écran,
pas via la moulinette.

---

## 7. À pratiquer (sur ta machine) {#pratique}

1. Crée un projet console, ajoute `Silk.NET.Windowing` + `Silk.NET.OpenGL`, et **ouvre une fenêtre**.
2. Dans `Render`, **change la couleur d'effacement** ; observe la fenêtre.
3. Ajoute `Silk.NET.Input` : ferme la fenêtre avec **Échap**.
4. (Avancé) Dessine un **triangle** : un VBO, un VAO, un vertex + fragment shader, `DrawArrays`.
5. Compare avec un moteur de plus haut niveau (MonoGame, Stride) pour mesurer ce que Silk.NET
   t'apprend du fonctionnement réel du GPU.

## Références externes

- Site officiel **Silk.NET** (dotnet.github.io/Silk.NET) et son dépôt d'exemples.
- *LearnOpenGL* (learnopengl.com) — concepts OpenGL transposables aux appels Silk.NET.
- Documentation **OpenGL** / **Vulkan** (Khronos).
