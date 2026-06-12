using System.Globalization;
using Microsoft.Extensions.Options;
using Producer.Service.Domain;
using Producer.Shared.Exchanges;

namespace Producer.Service.Application.Ticks.PayloadGenerateHandlers;

public class FuturesTickPayloadGenerationHandler(IOptions<PayloadGenerationOptions> payloadGenerationOptions)
    : ITickPayloadGenerateHandler
{
    private readonly Random _random = new();

    public bool CanHandle(ExchangeType exchangeType)
    {
        return exchangeType is ExchangeType.Futures;
    }

    public string Generate()
    {
        var symbol = _random.GetRandomString(payloadGenerationOptions.Value.FuturesSymbols);
        var source = _random.GetRandomString(payloadGenerationOptions.Value.FuturesSources);
        var price = _random.GetRandomDecimal(decimals: 2);
        var volume = _random.GetRandomDecimal(decimals: 0);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);

        return string.Create(CultureInfo.InvariantCulture,
            $"{symbol},{price.ToString(CultureInfo.InvariantCulture)},{volume.ToString(CultureInfo.InvariantCulture)},{timestamp},{source}");
    }
}