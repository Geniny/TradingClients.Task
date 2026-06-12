namespace Consumer.Service.Domain.Metrics;

public enum MetricType
{
    TicksReceived,
    TicksNormalized,
    TickDuplicates,
    TickWrites,
    TickErrors,
    ConnectionErrors,
}