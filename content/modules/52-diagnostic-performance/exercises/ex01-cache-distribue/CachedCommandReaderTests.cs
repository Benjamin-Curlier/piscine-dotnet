global using System;
global using System.Threading;
global using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class CachedCommandReaderTests
{
    [Fact]
    public async Task Repeated_read_uses_cache_and_propagates_cancellation_on_the_miss()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        await using var provider = services.BuildServiceProvider();
        var repository = new RecordingRepository();
        var reader = new CachedCommandReader(
            provider.GetRequiredService<HybridCache>(),
            repository);
        var id = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();

        var first = await reader.ReadAsync(id, cancellation.Token);
        var second = await reader.ReadAsync(id, CancellationToken.None);

        Assert.Equal(first, second);
        Assert.Equal(1, repository.CallCount);
        Assert.True(repository.ReceivedToken.CanBeCanceled);
    }

    private sealed class RecordingRepository : ICommandRepository
    {
        public int CallCount { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public Task<CommandDetails?> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            CallCount++;
            ReceivedToken = cancellationToken;
            return Task.FromResult<CommandDetails?>(new CommandDetails(id, "ready"));
        }
    }
}
