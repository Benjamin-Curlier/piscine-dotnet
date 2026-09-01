using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("asteria_tests")
        .WithUsername("asteria_test")
        .WithPassword(Guid.NewGuid().ToString("N"))
        .Build();

    public string ConnectionString => _database.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _database.StartAsync(cancellation.Token);
        var options = new DbContextOptionsBuilder<CommandDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var db = new CommandDbContext(options);
        await db.Database.MigrateAsync(cancellation.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _database.DisposeAsync();
    }
}
