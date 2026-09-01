using Domain;
using System;
using System.Collections.Generic;

namespace Infrastructure;

public sealed class MemoryInbox : IInbox
{
    private readonly HashSet<string> _messageIds = new(StringComparer.Ordinal);

    public bool TryRecord(string messageId) => _messageIds.Add(messageId);
}
