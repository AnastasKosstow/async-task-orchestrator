using System.Text.Json;
using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.Common.Execution;

public class WorkflowContext(Workflow workflow)
{
    public Workflow Workflow { get; } = workflow;

    public TOptions GetOptions<TOptions>() where TOptions : class
    {
        var options = JsonSerializer.Deserialize<TOptions>(Workflow.Payload);

        if (options is null)
        {
            throw new InvalidOperationException($"Unable to deserialize payload to {typeof(TOptions).Name}.");
        }

        return options;
    }
}

