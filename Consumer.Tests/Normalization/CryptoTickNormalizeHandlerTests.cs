using System.Text;
using Consumer.Service.Application.Ticks.Normalization.Handlers;
using Producer.Shared.Exchanges;

namespace Consumer.Tests.Normalization;

public sealed class CryptoTickNormalizeHandlerTests
{
    private readonly CryptoTickNormalizeHandler _sut = new();

    [Fact]
    public void Normalize_ValidPayload_ReturnsTickData()
    {
        var payload =
            Encoding.UTF8.GetBytes(s: """{"e":"trade","s":"BTCUSDT","p":"68432.50","q":"0.15","t":1717400000123}""");

        var result = _sut.Normalize(payload);

        Assert.Equal(ExchangeType.Crypto, result.Exchange);
        Assert.Equal("BTCUSDT", result.Ticker);
        Assert.Equal(expected: 68432.50m, result.Price);
        Assert.Equal(expected: 0.15m, result.Volume);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(milliseconds: 1717400000123), result.Timestamp);
    }

    [Fact]
    public void Normalize_EmptyPayload_ThrowsFormatException()
    {
        var payload = Array.Empty<byte>();

        var action = () => _sut.Normalize(payload);

        Assert.Throws<FormatException>(action);
    }
}