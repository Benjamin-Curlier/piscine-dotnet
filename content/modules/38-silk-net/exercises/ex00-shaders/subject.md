# ex00 — Deux étapes du pipeline

Complète deux shaders GLSL 330 core :

- `triangle.vert` reçoit `vec3 position` à `layout (location = 0)` et écrit
  `gl_Position = vec4(position, 1.0)` ;
- `triangle.frag` déclare une sortie `vec4 FragColor`, un `uniform vec4 tint`, puis affecte la
  couleur à la sortie.

La moulinette inspecte les contrats sans créer de contexte OpenGL. Dans l'atelier local, Silk.NET
compile ces chaînes avec l'API GL : les erreurs de syntaxe finales seront alors rapportées par le
driver.
