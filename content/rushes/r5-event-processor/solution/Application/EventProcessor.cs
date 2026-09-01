using Domain;
using System.Threading;
using System.Threading.Tasks;

namespace Application;

public sealed class EventProcessor
{
    private readonly IInbox _inbox;
    private readonly IAuditSink _audit;

    public EventProcessor(IInbox inbox, IAuditSink audit)
    {
        _inbox = inbox;
        _audit = audit;
    }

    public int Balance { get; private set; }

    public Task ProcessAsync(EventMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (message.Amount <= 0 || message.Type is not ("credit" or "debit"))
        {
            _audit.Write($"REJECTED {message.Id}");
            return Task.CompletedTask;
        }

        if (!_inbox.TryRecord(message.Id))
        {
            _audit.Write($"DUPLICATE {message.Id}");
            return Task.CompletedTask;
        }

        Balance += message.Type == "credit" ? message.Amount : -message.Amount;
        _audit.Write($"APPLIED {message.Id} BALANCE {Balance}");
        return Task.CompletedTask;
    }
}
