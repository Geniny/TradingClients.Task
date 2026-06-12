using Consumer.Service.Application.Metrics;
using Consumer.Service.Application.Ticks;
using Consumer.Service.Application.Ticks.Deduplication;
using Consumer.Service.Application.Ticks.Normalization;
using Consumer.Service.Application.Ticks.Storage;
using Consumer.Service.Domain.Metrics;
using Consumer.Service.Domain.Ticks;
using Moq;
using Producer.Shared.Exchanges;

namespace Consumer.Tests.Pipeline;

public sealed class TickProcessingPipelineTests
{
    [Fact]
    public async Task ProcessAsync_UniqueTick_CallsNormalizerDeduplicatorWriterAndMetric()
    {
        var payload = new byte[] { 1, 2, 3 };
        var token = new CancellationTokenSource().Token;
        var tick = CreateTick();

        var metricProvider = new Mock<IMetricProvider>(MockBehavior.Strict);
        var normalizer = new Mock<ITickNormalizer>(MockBehavior.Strict);
        var deduplicator = new Mock<ITickDeduplicator>(MockBehavior.Strict);
        var writer = new Mock<ITickWriter>(MockBehavior.Strict);

        normalizer
            .Setup(x => x.Normalize(ExchangeType.Crypto, payload))
            .Returns(tick);
        metricProvider
            .Setup(x => x.Increment(MetricType.TicksNormalized, 1));
        deduplicator
            .Setup(x => x.IsDuplicate(tick))
            .Returns(value: false);
        writer
            .Setup(x => x.WriteAsync(tick, token))
            .Returns(ValueTask.CompletedTask);

        var sut = new TickProcessingPipeline(
            metricProvider.Object,
            normalizer.Object,
            deduplicator.Object,
            writer.Object);

        await sut.ProcessAsync(ExchangeType.Crypto, payload, token);

        normalizer.Verify(x => x.Normalize(ExchangeType.Crypto, payload), Times.Once);
        metricProvider.Verify(x => x.Increment(MetricType.TicksNormalized, 1), Times.Once);
        deduplicator.Verify(x => x.IsDuplicate(tick), Times.Once);
        writer.Verify(x => x.WriteAsync(tick, token), Times.Once);
        metricProvider.Verify(x => x.Increment(MetricType.TickDuplicates, 1), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateTick_SkipsWriterAndIncrementsDuplicateMetric()
    {
        var payload = new byte[] { 4, 5, 6 };
        var token = new CancellationTokenSource().Token;
        var tick = CreateTick();

        var metricProvider = new Mock<IMetricProvider>(MockBehavior.Strict);
        var normalizer = new Mock<ITickNormalizer>(MockBehavior.Strict);
        var deduplicator = new Mock<ITickDeduplicator>(MockBehavior.Strict);
        var writer = new Mock<ITickWriter>(MockBehavior.Strict);

        normalizer
            .Setup(x => x.Normalize(ExchangeType.Futures, payload))
            .Returns(tick);
        metricProvider
            .Setup(x => x.Increment(MetricType.TicksNormalized, 1));
        deduplicator
            .Setup(x => x.IsDuplicate(tick))
            .Returns(value: true);
        metricProvider
            .Setup(x => x.Increment(MetricType.TickDuplicates, 1));

        var sut = new TickProcessingPipeline(
            metricProvider.Object,
            normalizer.Object,
            deduplicator.Object,
            writer.Object);

        await sut.ProcessAsync(ExchangeType.Futures, payload, token);

        normalizer.Verify(x => x.Normalize(ExchangeType.Futures, payload), Times.Once);
        metricProvider.Verify(x => x.Increment(MetricType.TicksNormalized, 1), Times.Once);
        deduplicator.Verify(x => x.IsDuplicate(tick), Times.Once);
        metricProvider.Verify(x => x.Increment(MetricType.TickDuplicates, 1), Times.Once);
        writer.Verify(x => x.WriteAsync(It.IsAny<TickData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static TickData CreateTick()
    {
        return new TickData
        {
            Exchange = ExchangeType.Crypto,
            Ticker = "BTCUSDT",
            Price = 68432.50m,
            Volume = 0.15m,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds: 1717400000123),
        };
    }
}