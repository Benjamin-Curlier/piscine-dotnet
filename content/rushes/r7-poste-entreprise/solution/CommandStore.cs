using System.ComponentModel.DataAnnotations.Concurrency;
using Microsoft.EntityFrameworkCore;

[Index(nameof(IdempotencyKey), IsUnique = true)]
public sealed class StoredCommand
{
    public Guid Id { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string Payload { get; init; }
    [ConcurrencyCheck]
    public long Version { get; set; }
}

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public required string Subject { get; init; }
    public required string Payload { get; init; }
}

public sealed class CommandStore(ProductDbContext db)
{
    public async Task<Guid> AddAsync(string key, string payload, CancellationToken cancellationToken)
    {
        var existing = await db.Commands.SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var command = new StoredCommand { Id = Guid.NewGuid(), IdempotencyKey = key, Payload = payload, Version = 1 };
        db.Commands.Add(command);
        db.Outbox.Add(new OutboxMessage { Id = Guid.NewGuid(), Subject = "asteria.commands", Payload = payload });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return command.Id;
    }
}

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<StoredCommand> Commands => Set<StoredCommand>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
}
