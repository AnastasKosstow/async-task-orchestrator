using AsyncTaskOrchestrator.RabbitMq.Settings;
using System.Collections.Concurrent;
using RabbitMQ.Client;

namespace AsyncTaskOrchestrator.RabbitMq;

public class RabbitMqConnection : IRabbitMqConnection, IDisposable
{
    private readonly BlockingCollection<IConnection> _pool;
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _createLock = new(1, 1);

    private int _count;
    private bool _disposed;

    private const int POOL_SIZE = 10;

    public RabbitMqConnection(RabbitMqSettings settings)
    {
        _pool = new BlockingCollection<IConnection>(new ConcurrentQueue<IConnection>(), POOL_SIZE);

        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(CreateConnectionString(settings)),
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
    }

    public IConnection GetConnection()
    {
        while (_pool.TryTake(out var connection))
        {
            if (connection.IsOpen)
            {
                _pool.Add(connection);
                return connection;
            }

            connection.Dispose();
            Interlocked.Decrement(ref _count);
        }

        return GetOrCreateConnection();
    }

    private IConnection GetOrCreateConnection()
    {
        if (_count < POOL_SIZE)
        {
            _createLock.Wait();
            try
            {
                if (_count < POOL_SIZE)
                {
                    var crtConnection = _connectionFactory.CreateConnection();
                    Interlocked.Increment(ref _count);
                    _pool.Add(crtConnection);
                    return crtConnection;
                }
            }
            finally
            {
                _createLock.Release();
            }
        }

        var connection = _pool.Take();
        if (connection.IsOpen)
        {
            _pool.Add(connection);
            return connection;
        }

        connection.Dispose();

        var newConnection = _connectionFactory.CreateConnection();
        _pool.Add(newConnection);
        return newConnection;
    }

    private static string CreateConnectionString(RabbitMqSettings settings)
    {
        ArgumentException.ThrowIfNullOrEmpty(settings.Username, nameof(settings.Username));
        ArgumentException.ThrowIfNullOrEmpty(settings.Password, nameof(settings.Password));
        ArgumentException.ThrowIfNullOrEmpty(settings.Host, nameof(settings.Host));

        return $"amqp://{settings.Username}:{settings.Password}@{settings.Host}:{settings.Port}";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pool.CompleteAdding();

        while (_pool.TryTake(out var connection))
        {
            connection.Dispose();
        }

        _pool.Dispose();
        _createLock.Dispose();
    }
}
