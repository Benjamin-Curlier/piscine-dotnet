using Microsoft.EntityFrameworkCore;

public sealed record CommandSummary(Guid Id, string Status, DateTimeOffset CreatedAt);
public sealed record CommandPage(IReadOnlyList<CommandSummary> Items, bool HasMore, DateTimeOffset? NextCursor);

public sealed class CommandQueries(CommandDbContext db)
{
    public async Task<CommandPage> ReadAsync(
        string status,
        DateTimeOffset? cursor,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = db.Commands.AsNoTracking().Where(command => command.Status == status);
        if (cursor is not null)
        {
            query = query.Where(command => command.CreatedAt > cursor);
        }

        var rows = await query
            .OrderBy(command => command.CreatedAt)
            .ThenBy(command => command.Id)
            .Select(command => new CommandSummary(command.Id, command.Status, command.CreatedAt))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > pageSize;
        var items = rows.Take(pageSize).ToList();
        var nextCursor = hasMore ? items[^1].CreatedAt : null;
        return new CommandPage(items, hasMore, nextCursor);
    }
}

public sealed class CommandEntity
{
    public Guid Id { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CommandDbContext(DbContextOptions<CommandDbContext> options) : DbContext(options)
{
    public DbSet<CommandEntity> Commands => Set<CommandEntity>();
}
