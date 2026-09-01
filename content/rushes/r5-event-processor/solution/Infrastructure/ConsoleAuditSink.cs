using Domain;
using System;

namespace Infrastructure;

public sealed class ConsoleAuditSink : IAuditSink
{
    public void Write(string entry) => Console.WriteLine(entry);
}
