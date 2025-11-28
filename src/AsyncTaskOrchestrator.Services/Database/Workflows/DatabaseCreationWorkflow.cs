using AsyncTaskOrchestrator.Common.Models;
using AsyncTaskOrchestrator.Services.Database.Options;
using AsyncTaskOrchestrator.Services.Workflows;
using AsyncTaskOrchestrator.Services.Workflows.Submitters;

namespace AsyncTaskOrchestrator.Services.Database.Workflows;

public class DatabaseCreationWorkflow(
    IWorkflowSubmitter<DatabaseCreateOptions> submitter
) : IDatabaseCreationWorkflow
{
    public async Task<WorkflowResponse> CreateDatabaseCreationWorkflowAsync(
        DatabaseCreateOptions options,
        CancellationToken cancellationToken)
    {
        var creationParams = new WorkflowCreationParams<DatabaseCreateOptions>
        {
            Name = $"database::{options.Name}",
            Type = WorkflowType.DatabaseCreation,
            Options = options
        };

        var workflow = await submitter.SubmitAsync(creationParams, cancellationToken);

        return new WorkflowResponse
        {
            WorkflowId = workflow.Id,
            Name = workflow.Name,
            Type = workflow.Type,
            Status = workflow.Status,
            CreatedAt = workflow.CreatedAt,
            QueuedAt = workflow.QueuedAt
        };
    }
}
