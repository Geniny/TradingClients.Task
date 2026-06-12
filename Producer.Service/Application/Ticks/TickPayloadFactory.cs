using Producer.Shared.Exchanges;

namespace Producer.Service.Application.Ticks;

public class TickPayloadFactory(IEnumerable<ITickPayloadGenerateHandler> generateHandlers) : ITickPayloadFactory
{
    private string? _lastPayload;

    public string Create(ExchangeType exchangeType, bool shouldBeDuplicate)
    {
        if (shouldBeDuplicate && _lastPayload is not null)
        {
            return _lastPayload;
        }

        var generateHandler = generateHandlers.FirstOrDefault(x => x.CanHandle(exchangeType));

        ArgumentNullException.ThrowIfNull(generateHandler, "Generate handler not found.");

        var payload = generateHandler.Generate();

        _lastPayload = payload;

        return payload;
    }
}