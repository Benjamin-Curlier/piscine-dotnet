# ex00 — Contrat natif généré

Déclare dans `NativeApi.cs` la fonction C suivante, sans l'appeler :

```c
int read_sensor(const char* name, double* value);
```

Contraintes :

- bibliothèque `asteria_native` ;
- `EntryPoint` égal à `read_sensor` ;
- nom managé `ReadSensor` ;
- paramètres `string name, out double value` et retour `int` ;
- UTF-8 explicite et `SetLastError = true` ;
- `[LibraryImport]`, classe et méthode `static partial` ;
- ni `[DllImport]` ni `unsafe`.

La moulinette inspecte le contrat sans charger la bibliothèque native.
