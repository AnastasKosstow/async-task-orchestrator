using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.Services.Workflows.Repositories;

public interface IWorkflowRepository
{
    Task CreateAsync(Workflow workflow, CancellationToken cancellationToken);
    Task<Workflow?> GetAsync(Guid workflowId, CancellationToken cancellationToken);
    Task SaveAsync(Workflow workflow, CancellationToken cancellationToken);
}
