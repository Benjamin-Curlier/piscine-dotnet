using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Hybrid;

public interface ICommandRepository
{
    Task<CommandDetails?> ReadAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record CommandDetails(Guid Id, string Status);

public sealed class CachedCommandReader(HybridCache cache, ICommandRepository repository)
{
    private static readonly Meter Meter = new("Asteria.Cache");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("cache.requests");

    public async Task<CommandDetails?> ReadAsync(Guid id, CancellationToken cancellationToken)
    {
        Requests.Add(1);
        return await cache.GetOrCreateAsync(
            $"commands:{id}",
            async cancel => await repository.ReadAsync(id, cancel),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromSeconds(30)
            },
            tags: ["commands"],
            cancellationToken: cancellationToken);
    }
}
