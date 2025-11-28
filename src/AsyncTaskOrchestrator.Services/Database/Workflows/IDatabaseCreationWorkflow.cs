using AsyncTaskOrchestrator.Common.Models;
using AsyncTaskOrchestrator.Services.Database.Options;

namespace AsyncTaskOrchestrator.Services.Database.Workflows;

public interface IDatabaseCreationWorkflow
{
    Task<WorkflowResponse> CreateDatabaseCreationWorkflowAsync(
        DatabaseCreateOptions options,
        CancellationToken cancellationToken
    );
}
