using Consumer.Service.Application.Ticks.Storage;
using Consumer.Service.Domain.Ticks;
using Npgsql;

namespace Consumer.Service.Infrastructure.Persistence.Ticks;

public sealed class TickRepository(
    IConfiguration configuration,
    ILogger<TickRepository> logger) : ITickRepository, IAsyncDisposable
{
    private const string InsertBatchSql =
        """
        INSERT INTO raw_ticks (source, ticker, price, volume, ticks_timestamp)
        SELECT s, t, p, v, ts
        FROM UNNEST(
            @sources::text[],
            @tickers::text[],
            @prices::numeric[],
            @volumes::numeric[],
            @timestamps::timestamptz[]) AS u(s, t, p, v, ts);
        """;

    private const string SchemaSql =
        """
        CREATE TABLE IF NOT EXISTS raw_ticks
        (
            id BIGSERIAL PRIMARY KEY,
            source TEXT NOT NULL,
            ticker TEXT NOT NULL,
            price NUMERIC(18, 8) NOT NULL,
            volume NUMERIC(18, 8) NOT NULL,
            ticks_timestamp TIMESTAMPTZ NOT NULL
        );
        """;

    private readonly NpgsqlDataSource _dataSource = BuildDataSource(configuration);

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(SchemaSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("Ensured PostgreSQL schema for raw_ticks");
    }

    public async Task<int> InsertBatchAsync(IReadOnlyCollection<TickData> ticks, CancellationToken cancellationToken)
    {
        if (ticks.Count == 0)
        {
            return 0;
        }

        var sources = new string[ticks.Count];
        var tickers = new string[ticks.Count];
        var prices = new decimal[ticks.Count];
        var volumes = new decimal[ticks.Count];
        var timestamps = new DateTimeOffset[ticks.Count];

        var index = 0;
        foreach (var tick in ticks)
        {
            sources[index] = tick.Exchange.ToString();
            tickers[index] = tick.Ticker;
            prices[index] = tick.Price;
            volumes[index] = tick.Volume;
            timestamps[index] = tick.Timestamp;
            index++;
        }

        await using var command = _dataSource.CreateCommand(InsertBatchSql);
        command.Parameters.Add(new NpgsqlParameter<string[]>("sources", sources));
        command.Parameters.Add(new NpgsqlParameter<string[]>("tickers", tickers));
        command.Parameters.Add(new NpgsqlParameter<decimal[]>("prices", prices));
        command.Parameters.Add(new NpgsqlParameter<decimal[]>("volumes", volumes));
        command.Parameters.Add(new NpgsqlParameter<DateTimeOffset[]>("timestamps", timestamps));

        await command.ExecuteNonQueryAsync(cancellationToken);

        return ticks.Count;
    }

    private static NpgsqlDataSource BuildDataSource(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, "Connection is not set.");

        return new NpgsqlDataSourceBuilder(connectionString).Build();
    }
}