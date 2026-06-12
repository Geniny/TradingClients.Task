using System.Net.WebSockets;

namespace Consumer.Service.Infrastructure.Ticks.Consuming;

public sealed class TicksConsumerWebSocketClient
{
    public async Task ConsumeAsync(
        Uri endpoint,
        Func<byte[], CancellationToken, Task> onMessage,
        CancellationToken cancellationToken)
    {
        using var webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(endpoint, cancellationToken);

        var buffer = new byte[4 * 1024];
        using var memoryStream = new MemoryStream();

        while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var segment = new ArraySegment<byte>(buffer);
            var result = await webSocket.ReceiveAsync(segment, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            memoryStream.Write(buffer, offset: 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            var payload = memoryStream.ToArray();

            memoryStream.SetLength(value: 0);

            await onMessage(payload, cancellationToken);
        }
    }
}