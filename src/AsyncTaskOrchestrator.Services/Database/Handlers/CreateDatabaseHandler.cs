using AsyncTaskOrchestrator.Common.Execution;
using AsyncTaskOrchestrator.Services.Database.Options;
using AsyncTaskOrchestrator.Services.Workflows;
using Microsoft.Extensions.Logging;

namespace AsyncTaskOrchestrator.Services.Database.Handlers;

public class CreateDatabaseHandler(ILogger<CreateDatabaseHandler> logger) : IWorkflowHandler
{
    public async Task<WorkflowResult> HandleAsync(WorkflowContext context, CancellationToken cancellationToken)
    {
        var options = context.GetOptions<DatabaseCreateOptions>();

        logger.LogInformation("Simulating database creation for {DatabaseName}", options.Name);

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        return WorkflowResult.Success($"Database {options.Name} created.");
    }
}

