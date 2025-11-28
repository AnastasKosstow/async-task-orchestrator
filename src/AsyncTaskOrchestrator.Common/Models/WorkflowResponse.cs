namespace AsyncTaskOrchestrator.Common.Models;

public class WorkflowResponse
{
    public Guid WorkflowId { get; init; }
    public string Name { get; init; } = string.Empty;
    public WorkflowType Type { get; init; }
    public WorkflowStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? QueuedAt { get; init; }
}
