using System.Collections.Concurrent;
using Consumer.Service.Domain.Metrics;

namespace Consumer.Service.Application.Metrics;

public class MetricProvider : IMetricProvider
{
    private readonly ConcurrentDictionary<MetricType, MetricCounter> _counters = new();

    public MetricProvider()
    {
        foreach (var metricType in Enum.GetValues<MetricType>())
        {
            _counters.TryAdd(metricType, new MetricCounter(metricType));
        }
    }

    public void Increment(MetricType type, long delta = 1)
    {
        var counter = GetCounter(type);
        counter.Increment(delta);
    }

    public long GetValue(MetricType type)
    {
        var counter = GetCounter(type);

        return counter.Value;
    }

    private MetricCounter GetCounter(MetricType type)
    {
        return _counters.TryGetValue(type, out var counter)
            ? counter
            : throw new NotSupportedException($"Metric type {type} is not supported.");
    }
}