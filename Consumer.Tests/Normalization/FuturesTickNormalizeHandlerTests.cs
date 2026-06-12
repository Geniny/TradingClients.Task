using System.Text;
using Consumer.Service.Application.Ticks.Normalization.Handlers;
using Producer.Shared.Exchanges;

namespace Consumer.Tests.Normalization;

public sealed class FuturesTickNormalizeHandlerTests
{
    private readonly FuturesTickNormalizeHandler _sut = new();

    [Fact]
    public void Normalize_ValidPayload_ReturnsTickData()
    {
        var payload = Encoding.UTF8.GetBytes("ESM4,6880.25,2,20240603-09:30:01.500,NYSE");

        var result = _sut.Normalize(payload);

        Assert.Equal(ExchangeType.Futures, result.Exchange);
        Assert.Equal("ESM4", result.Ticker);
        Assert.Equal(expected: 6880.25m, result.Price);
        Assert.Equal(expected: 2m, result.Volume);
        Assert.Equal(DateTimeOffset.Parse("2024-06-03T09:30:01.500+00:00"), result.Timestamp);
    }

    [Fact]
    public void Normalize_InvalidPayload_ThrowsFormatException()
    {
        var payload = Encoding.UTF8.GetBytes("ESM4,6880.25");

        var action = () => _sut.Normalize(payload);

        Assert.Throws<FormatException>(action);
    }
}