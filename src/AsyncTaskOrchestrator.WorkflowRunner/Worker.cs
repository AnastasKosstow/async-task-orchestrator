using AsyncTaskOrchestrator.Common.Execution;
using AsyncTaskOrchestrator.Common.Models;
using AsyncTaskOrchestrator.RabbitMq;
using AsyncTaskOrchestrator.RabbitMq.Settings;
using AsyncTaskOrchestrator.Services.Workflows;
using AsyncTaskOrchestrator.Services.Workflows.Repositories;
using AsyncTaskOrchestrator.Services.Workflows.Submitters.Publisher;
using AsyncTaskOrchestrator.WorkflowRunner.Exceptions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace AsyncTaskOrchestrator.WorkflowRunner;

public class Worker(
    IRabbitMqConnection connection,
    IOptions<RabbitMqSettings> settings,
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger
) : IHostedService
{
    private readonly IRabbitMqConnection _connection = connection;
    private readonly RabbitMqSettings _settings = settings.Value;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<Worker> _logger = logger;

    private bool _disposed;
    private IModel? _channel;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = _connection.GetConnection().CreateModel();

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        DeclareTopology(_channel);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += ProcessMessage;

        _channel.BasicConsume(
            queue: _settings.Queue.Name,
            autoAck: false,
            consumer: consumer
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        return Task.CompletedTask;
    }

    private async Task ProcessMessage(object model, BasicDeliverEventArgs eventArgs)
    {
        if (_channel == null || !_channel.IsOpen)
        {
            return;
        }

        var message = Deserialize(eventArgs.Body);
        if (message == null)
        {
            _channel.BasicAck(eventArgs.DeliveryTag, false);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWorkflowRepository>();

        try
        {
            await ProcessWorkflowAsync(message, repository, scope.ServiceProvider, CancellationToken.None);
            _channel.BasicAck(eventArgs.DeliveryTag, false);
        }
        catch (WorkflowHandlerNotRegisteredException ex)
        {
            _logger.LogError(ex, "No handler registered for workflow type {WorkflowType}", message.Type);

            await MarkWorkflowFailedAsync(message.WorkflowId, ex.Message, repository, CancellationToken.None);

            _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            var retryCount = eventArgs.BasicProperties?.Headers?.TryGetValue("x-retry-count", out var value) == true
                ? Convert.ToInt32(value)
                : 0;

            if (retryCount >= 3)
            {
                await MarkWorkflowFailedAsync(message.WorkflowId, ex.Message, repository, CancellationToken.None);
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
            else
            {
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }

    private async Task ProcessWorkflowAsync(WorkflowMessage message, IWorkflowRepository repository, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var handler = ResolveHandler(message.Type, provider);

        var workflow = CreateWorkflow(message);
        var context = new WorkflowContext(workflow);

        workflow.Status = WorkflowStatus.Running;
        workflow.StartedAt = DateTimeOffset.UtcNow;

        await repository.SaveAsync(workflow, cancellationToken);

        var result = await handler.HandleAsync(context, cancellationToken);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(result.Message)
                    ? $"Handler {handler.GetType().Name} failed without returning a message."
                    : result.Message);
        }

        workflow.Status = WorkflowStatus.Completed;
        workflow.CompletedAt = DateTimeOffset.UtcNow;
        workflow.Result = result.Message;

        await repository.SaveAsync(workflow, cancellationToken);

        _logger.LogInformation(
            "Workflow {WorkflowId} ({Name}) completed successfully: {Message}",
            workflow.Id,
            workflow.Name,
            workflow.Result);
    }

    private async Task MarkWorkflowFailedAsync(Guid id, string errorMessage, IWorkflowRepository repository, CancellationToken cancellationToken)
    {
        try
        {
            var workflow = await repository.GetAsync(id, cancellationToken);
            if (workflow != null)
            {
                workflow.Status = WorkflowStatus.Failed;
                workflow.CompletedAt = DateTimeOffset.UtcNow;
                workflow.Result = errorMessage;
                await repository.SaveAsync(workflow, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark workflow {WorkflowId} as failed", id);
        }
    }

    private static Workflow CreateWorkflow(WorkflowMessage message)
    {
        return new Workflow
        {
            Id = message.WorkflowId,
            Name = string.IsNullOrWhiteSpace(message.Name)
                ? $"workflow::{message.WorkflowId:N}"
                : message.Name,
            Type = message.Type,
            Payload = message.Payload,
            Status = WorkflowStatus.Running,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    private IWorkflowHandler ResolveHandler(WorkflowType type, IServiceProvider provider)
    {
        try
        {
            return provider.GetRequiredKeyedService<IWorkflowHandler>(type);
        }
        catch (InvalidOperationException ex)
        {
            throw new WorkflowHandlerNotRegisteredException(type, ex);
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

    private static WorkflowMessage? Deserialize(ReadOnlyMemory<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WorkflowMessage>(payload.Span);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _channel?.Dispose();
        }
    }
}
