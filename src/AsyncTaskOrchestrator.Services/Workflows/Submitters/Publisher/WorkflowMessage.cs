using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.Services.Workflows.Submitters.Publisher;

public record class WorkflowMessage
{
    public Guid WorkflowId { get; init; }
    public string Name { get; init; } = string.Empty;
    public WorkflowType Type { get; init; }
    public string Payload { get; init; } = string.Empty;
}
