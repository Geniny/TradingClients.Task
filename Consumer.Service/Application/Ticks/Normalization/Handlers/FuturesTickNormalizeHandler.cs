using System.Globalization;
using System.Text;
using Consumer.Service.Domain.Ticks;
using Producer.Shared.Exchanges;

namespace Consumer.Service.Application.Ticks.Normalization.Handlers;

public sealed class FuturesTickNormalizeHandler : ITickNormalizeHandler
{
    public bool CanHandle(ExchangeType exchange)
    {
        return exchange == ExchangeType.Futures;
    }

    /// <example>ESM4,6880.25,2,20240603-09:30:01.500,NYSE</example>
    public TickData Normalize(byte[] rawData)
    {
        if (rawData.Length == 0)
        {
            throw new FormatException("Futures payload is empty.");
        }

        var payload = Encoding.UTF8.GetString(rawData).Trim();
        var parts = payload.Split(separator: ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 5)
        {
            throw new FormatException(
                "Futures payload format is invalid. Expected: symbol,price,volume,timestamp,source.");
        }

        if (!decimal.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
        {
            throw new FormatException("Futures payload has invalid price.");
        }

        if (!decimal.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var volume))
        {
            throw new FormatException("Futures payload has invalid volume.");
        }

        if (!DateTimeOffset.TryParseExact(
                parts[3],
                "yyyyMMdd-HH:mm:ss.fff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var timestamp))
        {
            throw new FormatException("Futures payload has invalid timestamp.");
        }

        return new TickData
        {
            Exchange = ExchangeType.Futures,
            Ticker = parts[0],
            Price = price,
            Volume = volume,
            Timestamp = timestamp,
        };
    }
}