global using System;
global using System.Threading;
global using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class CommandStoreTests
{
    [Fact]
    public async Task A_stale_writer_is_reported_as_a_business_conflict()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CommandDbContext>()
            .UseSqlite(connection)
            .Options;
        var id = Guid.NewGuid();

        await using (var setup = new CommandDbContext(options))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.Commands.Add(new CommandEntity { Id = id, Status = "created", Version = 0 });
            await setup.SaveChangesAsync();
        }

        await using var firstContext = new CommandDbContext(options);
        await using var staleContext = new CommandDbContext(options);
        var first = await firstContext.Commands.SingleAsync(command => command.Id == id);
        var stale = await staleContext.Commands.SingleAsync(command => command.Id == id);
        first.Status = "sent";
        stale.Status = "cancelled";

        var firstResult = await new CommandStore(firstContext).SaveAsync(first, CancellationToken.None);
        var staleResult = await new CommandStore(staleContext).SaveAsync(stale, CancellationToken.None);

        Assert.Equal(SaveResult.Saved, firstResult);
        Assert.Equal(SaveResult.Conflict, staleResult);
    }
}
