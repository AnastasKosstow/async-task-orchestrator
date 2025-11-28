using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Channels;
using AsyncTaskOrchestrator.RabbitMq;
using AsyncTaskOrchestrator.RabbitMq.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AsyncTaskOrchestrator.Services.Workflows.Submitters.Publisher;

public sealed class QueuePublisher : IQueuePublisher, IAsyncDisposable, IDisposable
{
    private readonly IRabbitMqConnection _connection;
    private readonly ILogger<QueuePublisher> _logger;

    private readonly Channel<IModel> _channelPool;
    private readonly SemaphoreSlim _topologyLock = new(1, 1);
    private readonly RabbitMqSettings _settings;

    private const int POOL_SIZE = 10;
    private const int MAX_RETRIES = 3;

    private volatile bool _topologyDeclared;
    private volatile bool _disposed;
    private int _channelCount;

    public QueuePublisher(IRabbitMqConnection connection, IOptions<RabbitMqSettings> settings, ILogger<QueuePublisher> logger)
    {
        _connection = connection;
        _settings = settings?.Value;
        _logger = logger;

        _channelPool = Channel.CreateBounded<IModel>(new BoundedChannelOptions(POOL_SIZE)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task PublishAsync(WorkflowMessage message, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        ArgumentNullException.ThrowIfNull(message);

        cancellationToken.ThrowIfCancellationRequested();

        var attempt = 0;
        while (attempt < MAX_RETRIES)
        {
            var channel = await RentChannelAsync(cancellationToken);

            try
            {
                await EnsureTopologyAsync(channel, cancellationToken);

                Publish(channel, message);
                ReturnChannel(channel);
                return;
            }
            catch (Exception ex) when (IsTransientError(ex) && attempt < MAX_RETRIES - 1)
            {
                attempt++;
                DisposeChannel(channel);

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), cancellationToken);
            }
            catch
            {
                DisposeChannel(channel);
                throw;
            }
        }
    }

    private void Publish(IModel channel, WorkflowMessage message)
    {
        var properties = channel.CreateBasicProperties();

        properties.Persistent = true;
        properties.MessageId = message.WorkflowId.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.ContentType = "application/json";
        properties.Type = message.Type.ToString();
        properties.Headers = new Dictionary<string, object>
        {
            ["x-workflow-name"] = message.Name,
            ["x-created-at"] = DateTimeOffset.UtcNow.ToString("O")
        };

        var payload = JsonSerializer.SerializeToUtf8Bytes(message);

        channel.BasicPublish(
            exchange: _settings.Exchange.Name,
            routingKey: _settings.Queue.Name,
            mandatory: false,
            basicProperties: properties,
            body: payload);
    }

    private async ValueTask<IModel> RentChannelAsync(CancellationToken cancellationToken)
    {
        while (_channelPool.Reader.TryRead(out var pooledChannel))
        {
            if (pooledChannel.IsOpen)
            {
                return pooledChannel;
            }

            DisposeChannel(pooledChannel);
        }

        if (_channelCount < POOL_SIZE)
        {
            var created = TryCreateChannel(out var newChannel);
            if (created && newChannel != null)
            {
                return newChannel;
            }
        }

        try
        {
            var channel = await _channelPool.Reader.ReadAsync(cancellationToken);

            if (channel.IsOpen)
            {
                return channel;
            }

            DisposeChannel(channel);
            return await RentChannelAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            throw new ObjectDisposedException(nameof(QueuePublisher));
        }
    }

    private void ReturnChannel(IModel channel)
    {
        if (_disposed || !channel.IsOpen)
        {
            DisposeChannel(channel);
            return;
        }

        if (!_channelPool.Writer.TryWrite(channel))
        {
            DisposeChannel(channel);
        }
    }

    private bool TryCreateChannel(out IModel? channel)
    {
        channel = null;

        var currentCount = Volatile.Read(ref _channelCount);

        if (currentCount >= POOL_SIZE)
        {
            return false;
        }

        if (Interlocked.CompareExchange(ref _channelCount, currentCount + 1, currentCount) != currentCount)
        {
            return false;
        }

        try
        {
            var connection = _connection.GetConnection();
            channel = connection.CreateModel();

            return true;
        }
        catch (Exception ex)
        {
            Interlocked.Decrement(ref _channelCount);
            throw;
        }
    }

    private void DisposeChannel(IModel channel)
    {
        Interlocked.Decrement(ref _channelCount);

        try
        {
            channel.Close();
            channel.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing channel");
        }
    }

    private async Task EnsureTopologyAsync(IModel channel, CancellationToken cancellationToken)
    {
        if (_topologyDeclared)
        {
            return;
        }

        await _topologyLock.WaitAsync(cancellationToken);

        try
        {
            if (_topologyDeclared)
            {
                return;
            }

            DeclareTopology(channel);
            _topologyDeclared = true;
        }
        finally
        {
            _topologyLock.Release();
        }
    }

    private void DeclareTopology(IModel channel)
    {
        if (_settings.Exchange.Declare)
        {
            channel.ExchangeDeclare(
                exchange: _settings.Exchange.Name,
                type: _settings.Exchange.Type,
                durable: _settings.Exchange.Durable,
                autoDelete: _settings.Exchange.AutoDelete
            );
        }

        if (_settings.Queue.Declare)
        {
            channel.QueueDeclare(
                queue: _settings.Queue.Name,
                durable: _settings.Queue.Durable,
                exclusive: _settings.Queue.Exclusive,
                autoDelete: _settings.Queue.AutoDelete
            );
        }

        channel.QueueBind(
            queue: _settings.Queue.Name,
            exchange: _settings.Exchange.Name,
            routingKey: _settings.Queue.Name
        );
    }

    private static bool IsTransientError(Exception ex)
    {
        return ex is AlreadyClosedException
            or BrokerUnreachableException
            or IOException
            or SocketException;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _channelPool.Writer.Complete();

        while (_channelPool.Reader.TryRead(out var channel))
        {
            DisposeChannel(channel);
        }

        _topologyLock.Dispose();

        await ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
