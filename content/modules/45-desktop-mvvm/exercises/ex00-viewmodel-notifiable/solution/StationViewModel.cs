using System.ComponentModel;
using System.Runtime.CompilerServices;

public interface IStationReader
{
    Task<string> ReadStatusAsync(CancellationToken cancellationToken);
}

public sealed class StationViewModel(IStationReader reader) : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _status = "Inconnu";

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        private set { _status = value; OnPropertyChanged(); }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            Status = await reader.ReadStatusAsync(cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
