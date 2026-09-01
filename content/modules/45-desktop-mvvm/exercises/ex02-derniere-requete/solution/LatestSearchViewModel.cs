public interface ISearchClient
{
    Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellationToken);
}

public sealed class LatestSearchViewModel(ISearchClient client)
{
    private int _revision;
    private CancellationTokenSource? _current;

    public IReadOnlyList<string> Results { get; private set; } = [];

    public async Task SearchAsync(string query, CancellationToken cancellationToken)
    {
        var revision = Interlocked.Increment(ref _revision);
        var current = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var previous = Interlocked.Exchange(ref _current, current);
        previous?.Cancel();
        previous?.Dispose();

        try
        {
            var results = await client.SearchAsync(query, current.Token);
            if (revision != Volatile.Read(ref _revision))
            {
                return;
            }

            Results = results;
        }
        catch (OperationCanceledException) when (current.IsCancellationRequested)
        {
        }
    }
}
