using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Producer.Service.Domain;
using Producer.Shared.Exchanges;

namespace Producer.Service.Application.Ticks.PayloadGenerateHandlers;

public class CryptoTickPayloadGenerationHandler(IOptions<PayloadGenerationOptions> payloadGenerationOptions)
    : ITickPayloadGenerateHandler
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Random _random = new();

    public bool CanHandle(ExchangeType exchangeType)
    {
        return exchangeType is ExchangeType.Crypto;
    }

    public string Generate()
    {
        var symbol = _random.GetRandomString(payloadGenerationOptions.Value.Symbols);
        var price = _random.GetRandomDecimal(decimals: 2);
        var volume = _random.GetRandomDecimal(decimals: 2);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return JsonSerializer.Serialize(new
        {
            e = "trade",
            s = symbol,
            p = price.ToString("0.00", CultureInfo.InvariantCulture),
            q = volume.ToString("0.00", CultureInfo.InvariantCulture),
            t = timestamp,
        }, _jsonOptions);
    }
}