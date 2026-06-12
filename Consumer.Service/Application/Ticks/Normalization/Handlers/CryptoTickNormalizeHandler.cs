using System.Globalization;
using System.Text;
using System.Text.Json;
using Consumer.Service.Domain.Ticks;
using Producer.Shared.Exchanges;

namespace Consumer.Service.Application.Ticks.Normalization.Handlers;

public sealed class CryptoTickNormalizeHandler : ITickNormalizeHandler
{
    public bool CanHandle(ExchangeType exchange)
    {
        return exchange == ExchangeType.Crypto;
    }

    /// <example> {"e":"trade","s":"BTCUSDT","p":"68432.50","q":"0.15","t":1717400000123} </example>
    public TickData Normalize(byte[] rawData)
    {
        if (rawData.Length == 0)
        {
            throw new FormatException("Crypto payload is empty.");
        }

        var payload = Encoding.UTF8.GetString(rawData);
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var symbol = ReadString(root, "s")
                     ?? throw new FormatException("Crypto payload does not contain symbol.");

        var price = ReadDecimal(root, "p")
                    ?? throw new FormatException("Crypto payload does not contain price.");

        var volume = ReadDecimal(root, "q")
                     ?? throw new FormatException("Crypto payload does not contain volume.");

        var timestamp = ReadTimestamp(root, "t")
                        ?? throw new FormatException("Crypto payload does not contain timestamp.");

        return new TickData
        {
            Exchange = ExchangeType.Crypto,
            Ticker = symbol,
            Price = price,
            Volume = volume,
            Timestamp = timestamp,
        };
    }

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText().Trim(trimChar: '"');
    }

    private static decimal? ReadDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numberValue))
        {
            return numberValue;
        }

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var stringValue))
        {
            return stringValue;
        }

        return null;
    }

    private static DateTimeOffset? ReadTimestamp(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var numericTimestamp))
        {
            return numericTimestamp > 2_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(numericTimestamp)
                : DateTimeOffset.FromUnixTimeSeconds(numericTimestamp);
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var timestampString = value.GetString();
            if (DateTimeOffset.TryParse(timestampString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                    out var parsed))
            {
                return parsed;
            }

            if (long.TryParse(timestampString, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var numericFromString))
            {
                return numericFromString > 2_000_000_000
                    ? DateTimeOffset.FromUnixTimeMilliseconds(numericFromString)
                    : DateTimeOffset.FromUnixTimeSeconds(numericFromString);
            }
        }

        return null;
    }
}