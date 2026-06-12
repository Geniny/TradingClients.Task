using Consumer.Service.Domain.Ticks;
using Producer.Shared.Exchanges;

namespace Consumer.Service.Application.Ticks.Normalization;

public interface ITickNormalizer
{
    public TickData Normalize(ExchangeType exchange, byte[] rawData);
}