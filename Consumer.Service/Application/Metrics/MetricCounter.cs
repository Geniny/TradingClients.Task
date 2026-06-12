using Consumer.Service.Domain.Metrics;

namespace Consumer.Service.Application.Metrics;

public class MetricCounter
{
    private long _counter;

    public MetricCounter(MetricType metricType)
    {
        MetricType = metricType;
    }

    public MetricType MetricType { get; }

    public long Value => Interlocked.Read(ref _counter);

    public long Increment(long delta = 1)
    {
        return Interlocked.Add(ref _counter, delta);
    }

    public long Reset(long value = 0)
    {
        return Interlocked.Exchange(ref _counter, value);
    }
}