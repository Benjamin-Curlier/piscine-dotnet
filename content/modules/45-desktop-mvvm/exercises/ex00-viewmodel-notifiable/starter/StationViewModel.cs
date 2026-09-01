using System.ComponentModel;

public interface IStationReader
{
    Task<string> ReadStatusAsync(CancellationToken cancellationToken);
}

// TODO: ViewModel observable et asynchrone.
