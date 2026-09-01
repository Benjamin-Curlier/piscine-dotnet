using Domain;
using System.Collections.Generic;

namespace Infrastructure;

public sealed class MemoryInbox : IInbox
{
    // TODO : mémorise les ids réellement appliqués.
    public bool TryRecord(string messageId) => true;
}
