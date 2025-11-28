using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.Services.Workflows.Factories;

public interface IWorkflowFactory<TOptions>
    where TOptions : class
{
    Workflow Create(WorkflowCreationParams<TOptions> creationParams);
}
