using Consumer.Service.Application.Ticks.Deduplication;
using Consumer.Service.Domain.Ticks;
using Microsoft.Extensions.Options;
using Producer.Shared.Exchanges;

namespace Consumer.Tests.Deduplication;

public sealed class TickDeduplicatorWithExpirationTests
{
    [Fact]
    public void IsDuplicate_SameTickTwice_ReturnsFalseThenTrue()
    {
        var options = Options.Create(new TicksConsumeOptions
        {
            ProcessedTicksExpirationPeriod = TimeSpan.FromMinutes(minutes: 5),
        });
        var sut = new TickDeduplicatorWithExpiration(options, TimeProvider.System);
        var tick = new TickData
        {
            Exchange = ExchangeType.Crypto,
            Ticker = "BTCUSDT",
            Price = 68432.50m,
            Volume = 0.15m,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds: 1717400000123),
        };

        var first = sut.IsDuplicate(tick);
        var second = sut.IsDuplicate(tick);

        Assert.False(first);
        Assert.True(second);
    }
}