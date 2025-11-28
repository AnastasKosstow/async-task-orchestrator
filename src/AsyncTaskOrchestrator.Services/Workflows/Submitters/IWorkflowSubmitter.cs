using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.Services.Workflows.Submitters;

public interface IWorkflowSubmitter<TOptions>
    where TOptions : class
{
    Task<Workflow> SubmitAsync(WorkflowCreationParams<TOptions> creationParams, CancellationToken cancellationToken);
}
