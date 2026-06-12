using Consumer.Service.Domain.Ticks;
using Producer.Shared.Exchanges;

namespace Consumer.Service.Application.Ticks.Normalization;

public class TickNormalizer(IEnumerable<ITickNormalizeHandler> exchangeHandlers) : ITickNormalizer
{
    public TickData Normalize(ExchangeType exchange, byte[] rawData)
    {
        foreach (var exchangeHandler in exchangeHandlers)
        {
            if (exchangeHandler.CanHandle(exchange))
            {
                return exchangeHandler.Normalize(rawData);
            }
        }

        throw new NotImplementedException($"{exchange} is not supported.");
    }
}