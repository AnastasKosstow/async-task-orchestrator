namespace AsyncTaskOrchestrator.Services.Workflows.Submitters.Publisher;

public interface IQueuePublisher
{
    Task PublishAsync(WorkflowMessage message, CancellationToken cancellationToken);
}
