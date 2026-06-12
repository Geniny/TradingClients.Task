using Consumer.Service.Domain.Ticks;
using Producer.Shared.Exchanges;

namespace Consumer.Service.Application.Ticks.Normalization;

public interface ITickNormalizeHandler
{
    public bool CanHandle(ExchangeType exchange);
    public TickData Normalize(byte[] rawData);
}