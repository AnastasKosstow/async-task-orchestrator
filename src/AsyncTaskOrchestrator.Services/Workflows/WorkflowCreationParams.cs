using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.Services.Workflows;

public class WorkflowCreationParams<TOptions> where TOptions : class
{
    public string Name { get; init; } = string.Empty;
    public WorkflowType Type { get; set; }
    public TOptions Options { get; init; } = null!;
}
