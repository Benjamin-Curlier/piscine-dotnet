# ex01 — Dispatcher testable

Déclare `IUiDispatcher.InvokeAsync(Action, CancellationToken)`. `StatusPresenter` reçoit cette
abstraction et sa méthode `PublishAsync` déclenche `StatusChanged` uniquement dans l'action remise au
dispatcher.

Le code métier ne doit pas capturer directement un dispatcher ou un `SynchronizationContext` global.
