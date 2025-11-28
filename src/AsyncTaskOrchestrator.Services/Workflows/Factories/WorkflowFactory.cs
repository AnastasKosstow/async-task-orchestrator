using System.Text.Json;
using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.Services.Workflows.Factories;

public class WorkflowFactory<TOptions> : IWorkflowFactory<TOptions>
    where TOptions : class
{
    public Workflow Create(WorkflowCreationParams<TOptions> creationParams)
    {
        ArgumentNullException.ThrowIfNull(creationParams);
        ArgumentNullException.ThrowIfNull(creationParams.Options);

        return new Workflow
        {
            Name = creationParams.Name,
            Type = creationParams.Type,
            Payload = JsonSerializer.Serialize(creationParams.Options)
        };
    }
}
