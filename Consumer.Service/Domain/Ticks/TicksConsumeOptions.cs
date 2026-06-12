namespace Consumer.Service.Domain.Ticks;

public sealed class TicksConsumeOptions
{
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(seconds: 15);
    public TimeSpan ProcessedTicksExpirationPeriod { get; set; } = TimeSpan.FromSeconds(seconds: 300);
    public List<TicksConsumerClientConfiguration> Clients { get; set; } = [];
}