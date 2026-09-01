namespace Domain;

public interface IAuditSink
{
    void Write(string entry);
}
