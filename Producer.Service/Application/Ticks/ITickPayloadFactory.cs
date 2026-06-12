using Producer.Shared.Exchanges;

namespace Producer.Service.Application.Ticks;

public interface ITickPayloadFactory
{
    public string Create(ExchangeType exchangeType, bool shouldBeDuplicate);
}