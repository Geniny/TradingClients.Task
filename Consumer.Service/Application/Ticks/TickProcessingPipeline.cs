using Consumer.Service.Application.Metrics;
using Consumer.Service.Application.Ticks.Deduplication;
using Consumer.Service.Application.Ticks.Normalization;
using Consumer.Service.Application.Ticks.Storage;
using Consumer.Service.Domain.Metrics;
using Producer.Shared.Exchanges;

namespace Consumer.Service.Application.Ticks;

public sealed class TickProcessingPipeline(
    IMetricProvider metricProvider,
    ITickNormalizer tickNormalizer,
    ITickDeduplicator tickDeduplicator,
    ITickWriter tickWriter)
{
    public ValueTask ProcessAsync(ExchangeType exchange, byte[] payload, CancellationToken cancellationToken)
    {
        var tickData = tickNormalizer.Normalize(exchange, payload);
        metricProvider.Increment(MetricType.TicksNormalized);

        var isDuplicate = tickDeduplicator.IsDuplicate(tickData);
        if (isDuplicate)
        {
            metricProvider.Increment(MetricType.TickDuplicates);

            return ValueTask.CompletedTask;
        }

        return tickWriter.WriteAsync(tickData, cancellationToken);
    }
}