using Producer.Shared.Exchanges;

namespace Consumer.Service.Domain.Ticks;

public class TicksConsumerClientConfiguration
{
    public string ClientId { get; set; }
    public string ProducerUrl { get; set; }
    public ExchangeType ExchangeType { get; set; }
}