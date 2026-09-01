# ex00 — ViewModel réactif

Implémente `StationViewModel` avec `INotifyPropertyChanged`, les propriétés `IsBusy` et `Status`, puis
une méthode `RefreshAsync(CancellationToken)`. Elle appelle le port `IStationReader`, publie le statut
reçu et remet toujours `IsBusy` à `false`.

N'utilise aucun blocage synchrone ni `Thread.Sleep`.
