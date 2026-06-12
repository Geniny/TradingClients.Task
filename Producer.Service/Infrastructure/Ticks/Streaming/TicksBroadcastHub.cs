using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace Producer.Service.Infrastructure.Ticks.Streaming;

public sealed class TicksBroadcastHub(ILogger<TicksBroadcastHub> logger)
{
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    public int ConnectedClientsCount => _clients.Count;

    public async Task HandleClientAsync(string clientId, WebSocket socket, CancellationToken cancellationToken)
    {
        _clients[clientId] = socket;

        logger.LogInformation("Client connected: {ClientId}. Total Connected: {Count}", clientId, _clients.Count);

        var buffer = new byte[256];

        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        catch (WebSocketException ex)
        {
            logger.LogWarning(ex, "Client receive failed: {ClientId}", clientId);
        }
        finally
        {
            _clients.TryRemove(clientId, out _);

            await CloseSocketSafelyAsync(socket, cancellationToken);

            logger.LogInformation("Client disconnected: {ClientId}. Total Connected: {Count}", clientId,
                _clients.Count);
        }
    }

    public async Task BroadcastAsync(string payload, CancellationToken cancellationToken)
    {
        if (_clients.IsEmpty)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(payload);
        var segment = new ArraySegment<byte>(bytes);
        var staleClients = new List<string>();

        foreach (var (clientId, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open)
            {
                staleClients.Add(clientId);
                continue;
            }

            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException)
            {
                logger.LogWarning(ex, "WS send failed for client: {ClientId}", clientId);
                staleClients.Add(clientId);
            }
        }

        foreach (var staleClient in staleClients)
        {
            if (_clients.TryRemove(staleClient, out var staleSocket))
            {
                await CloseSocketSafelyAsync(staleSocket, cancellationToken);
            }
        }
    }

    private static async Task CloseSocketSafelyAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", cancellationToken);
            }
            catch
            {
                // Ignore errors while closing stale sockets.
            }
        }

        socket.Dispose();
    }
}