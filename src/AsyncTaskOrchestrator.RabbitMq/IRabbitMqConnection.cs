using RabbitMQ.Client;

namespace AsyncTaskOrchestrator.RabbitMq;

public interface IRabbitMqConnection : IDisposable
{
    IConnection GetConnection();
}
