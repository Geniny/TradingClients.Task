using System.Threading.Channels;
using Consumer.Service.Domain.Ticks;

namespace Consumer.Service.Application.Ticks.Storage;

public sealed class TickWriteQueue
{
    private readonly Channel<TickData> _channel = Channel.CreateBounded<TickData>(
        new BoundedChannelOptions(capacity: 10_000)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

    public ChannelReader<TickData> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(TickData tickData, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(tickData, cancellationToken);
    }
}