using Producer.Shared.Exchanges;

namespace Producer.Service.Application.Ticks;

public interface ITickPayloadGenerateHandler
{
    public bool CanHandle(ExchangeType exchangeType);
    public string Generate();
}