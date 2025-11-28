using AsyncTaskOrchestrator.Common.Execution;

namespace AsyncTaskOrchestrator.Services.Workflows;

public interface IWorkflowHandler
{
    Task<WorkflowResult> HandleAsync(WorkflowContext context, CancellationToken cancellationToken);
}
