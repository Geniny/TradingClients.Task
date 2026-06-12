using System.Collections.Concurrent;
using Consumer.Service.Domain.Ticks;
using Microsoft.Extensions.Options;

namespace Consumer.Service.Application.Ticks.Deduplication;

public sealed class TickDeduplicatorWithExpiration(IOptions<TicksConsumeOptions> options, TimeProvider timeProvider)
    : ITickDeduplicator
{
    private readonly TimeSpan _expirationPeriod = options.Value.ProcessedTicksExpirationPeriod;
    private readonly ConcurrentDictionary<int, DateTimeOffset> _tickExpiration = new();

    public bool IsDuplicate(TickData tickData)
    {
        CleanupExpired();

        var tickHash = tickData.GetHashCode();
        var expiresAt = timeProvider.GetUtcNow() + _expirationPeriod;

        if (_tickExpiration.TryAdd(tickHash, expiresAt))
        {
            return false;
        }

        return true;
    }

    //лучше конечно так не делать, блочит словарь
    private void CleanupExpired()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var (hash, expiration) in _tickExpiration)
        {
            if (expiration <= now)
            {
                _tickExpiration.TryRemove(hash, out _);
            }
        }
    }
}