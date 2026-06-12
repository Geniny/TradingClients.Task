using Producer.Shared.Exchanges;

namespace Consumer.Service.Domain.Ticks;

public sealed class TickData
{
    public required ExchangeType Exchange { get; init; }
    public required string Ticker { get; init; }
    public required decimal Price { get; init; }
    public required decimal Volume { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public override int GetHashCode()
    {
        return HashCode.Combine(Exchange, Ticker, Price, Volume, Timestamp.ToUnixTimeMilliseconds());
    }
}