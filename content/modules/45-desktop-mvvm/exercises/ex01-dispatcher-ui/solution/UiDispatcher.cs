public interface IUiDispatcher
{
    Task InvokeAsync(Action action, CancellationToken cancellationToken);
}

public sealed class StatusPresenter(IUiDispatcher dispatcher)
{
    public event Action<string>? StatusChanged;

    public Task PublishAsync(string status, CancellationToken cancellationToken) =>
        dispatcher.InvokeAsync(() => StatusChanged?.Invoke(status), cancellationToken);
}
