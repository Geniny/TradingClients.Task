namespace Consumer.Service.Domain.Ticks;

public sealed class TicksWriteOptions
{
    public int WriteSize { get; set; } = 200;
    public TimeSpan WriteTimeout { get; set; } = TimeSpan.FromSeconds(seconds: 5);
}