using AsyncTaskOrchestrator.Common.Models;
using AsyncTaskOrchestrator.Services.Workflows.Factories;
using AsyncTaskOrchestrator.Services.Workflows.Repositories;
using AsyncTaskOrchestrator.Services.Workflows.Submitters.Publisher;

namespace AsyncTaskOrchestrator.Services.Workflows.Submitters;

public class WorkflowSubmitter<TOptions>(
    IWorkflowFactory<TOptions> factory,
    IWorkflowRepository repository,
    IQueuePublisher publisher
) : IWorkflowSubmitter<TOptions> where TOptions : class
{
    public async Task<Workflow> SubmitAsync(WorkflowCreationParams<TOptions> creationParams, CancellationToken cancellationToken)
    {
        var workflow = factory.Create(creationParams);

        await repository.CreateAsync(workflow, cancellationToken);

        workflow.Status = WorkflowStatus.Queued;
        workflow.QueuedAt = DateTimeOffset.UtcNow;

        await publisher.PublishAsync(new WorkflowMessage
        {
            WorkflowId = workflow.Id,
            Name = workflow.Name,
            Payload = workflow.Payload,
            Type = workflow.Type
        }, cancellationToken);

        await repository.SaveAsync(workflow, cancellationToken);

        return workflow;
    }
}
