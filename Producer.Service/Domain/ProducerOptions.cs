using Producer.Shared.Exchanges;

namespace Producer.Service.Domain;

public sealed class ProducerOptions
{
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Crypto;
    public int TicksRatePerSecond { get; set; } = 90;
    public int DuplicateEveryNTick { get; set; } = 25;
}