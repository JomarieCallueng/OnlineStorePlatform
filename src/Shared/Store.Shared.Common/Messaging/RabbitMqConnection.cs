using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Store.Shared.Common.Messaging;

/// <summary>
/// Owns a single long-lived RabbitMQ connection, created lazily and recreated if it drops.
/// Thread-safe; shared as a singleton.
/// </summary>
public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly RabbitMqOptions _opts;
    private readonly ILogger<RabbitMqConnection> _log;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<RabbitMqOptions> opts, ILogger<RabbitMqConnection> log)
    {
        _opts = opts.Value;
        _log = log;
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            var factory = new ConnectionFactory
            {
                HostName = _opts.Host,
                Port = _opts.Port,
                UserName = _opts.User,
                Password = _opts.Password,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            _log.LogInformation("Opening RabbitMQ connection to {Host}:{Port}", _opts.Host, _opts.Port);
            _connection = await factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _gate.Dispose();
    }
}
