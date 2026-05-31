// Lis N, puis N messages ; un Sujet diffuse chacun aux observateurs A puis B (préfixes "[A] "/"[B] ").
// Astuce : interface IObservateur { void Notifier(string message); } ; Sujet a une List<IObservateur>.
