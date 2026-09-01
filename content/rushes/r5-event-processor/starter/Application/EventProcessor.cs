using Domain;
using System.Threading;
using System.Threading.Tasks;

namespace Application;

public sealed class EventProcessor
{
    public int Balance { get; private set; }

    // TODO : injecte inbox et audit, puis implémente validation, déduplication et effet métier.
    public Task ProcessAsync(EventMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
