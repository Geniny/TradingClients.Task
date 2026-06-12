using Producer.Service.Application.Ticks;
using Producer.Service.Application.Ticks.PayloadGenerateHandlers;
using Producer.Service.Domain;
using Producer.Service.Infrastructure.Ticks.Producing;
using Producer.Service.Infrastructure.Ticks.Streaming;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<ProducerOptions>(builder.Configuration.GetSection("Producer"));
builder.Services.Configure<PayloadGenerationOptions>(builder.Configuration.GetSection("PayloadGeneration"));

builder.Services.AddSingleton<TicksBroadcastHub>();
builder.Services.AddSingleton<ITickPayloadFactory, TickPayloadFactory>();
builder.Services.AddSingleton<ITickPayloadGenerateHandler, CryptoTickPayloadGenerationHandler>();
builder.Services.AddSingleton<ITickPayloadGenerateHandler, FuturesTickPayloadGenerationHandler>();

builder.Services.AddHostedService<TicksProducerService>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(seconds: 15),
});

app.Map("/ws", async context =>

{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        await context.Response.WriteAsync("WebSocket endpoint expects WS upgrade.");

        return;
    }

    var socket = await context.WebSockets.AcceptWebSocketAsync();

    var hub = context.RequestServices.GetRequiredService<TicksBroadcastHub>();

    var clientId = context.Connection.Id;

    await hub.HandleClientAsync(clientId, socket, context.RequestAborted);
});

app.MapDefaultEndpoints();

await app.RunAsync();