namespace Domain;

public interface IInbox
{
    bool TryRecord(string messageId);
}
