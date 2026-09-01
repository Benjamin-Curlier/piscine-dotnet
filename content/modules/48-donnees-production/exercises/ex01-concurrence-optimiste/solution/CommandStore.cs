using System.ComponentModel.DataAnnotations.Concurrency;
using Microsoft.EntityFrameworkCore;

public sealed class CommandEntity
{
    public Guid Id { get; init; }

    public required string Status { get; set; }

    [ConcurrencyCheck]
    public long Version { get; set; }
}

public enum SaveResult
{
    Saved,
    Conflict
}

public sealed class CommandStore(CommandDbContext db)
{
    public async Task<SaveResult> SaveAsync(CommandEntity command, CancellationToken cancellationToken)
    {
        command.Version++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return SaveResult.Saved;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SaveResult.Conflict;
        }
    }
}

public sealed class CommandDbContext(DbContextOptions<CommandDbContext> options) : DbContext(options)
{
    public DbSet<CommandEntity> Commands => Set<CommandEntity>();
}
