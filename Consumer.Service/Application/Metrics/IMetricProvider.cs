using Consumer.Service.Domain.Metrics;

namespace Consumer.Service.Application.Metrics;

public interface IMetricProvider
{
    void Increment(MetricType type, long delta = 1);
    long GetValue(MetricType type);
}