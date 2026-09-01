using System.ComponentModel;
using System.Runtime.CompilerServices;

public interface ICommandGateway
{
    Task<Guid> SubmitAsync(string payload, CancellationToken cancellationToken);
}

public interface IUiDispatcher
{
    Task InvokeAsync(Action action, CancellationToken cancellationToken);
}

public sealed class DesktopViewModel(ICommandGateway gateway, IUiDispatcher dispatcher) : INotifyPropertyChanged
{
    private int _revision;
    private CancellationTokenSource? _current;
    private string _status = "Prêt";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Status
    {
        get => _status;
        private set { _status = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status))); }
    }

    public async Task SubmitAsync(string payload, CancellationToken cancellationToken)
    {
        var revision = Interlocked.Increment(ref _revision);
        var current = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previous = Interlocked.Exchange(ref _current, current);
        previous?.Cancel();
        previous?.Dispose();

        try
        {
            var id = await gateway.SubmitAsync(payload, current.Token);
            if (revision == Volatile.Read(ref _revision))
            {
                await dispatcher.InvokeAsync(() => Status = $"Acceptée {id}", current.Token);
            }
        }
        catch (OperationCanceledException) when (current.IsCancellationRequested)
        {
        }
    }
}
