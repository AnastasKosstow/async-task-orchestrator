namespace AsyncTaskOrchestrator.Common.Models;

public class Workflow
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public WorkflowType Type { get; init; } = WorkflowType.Unknown;
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Created;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public string Payload { get; init; } = string.Empty;

    public DateTimeOffset? QueuedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Result { get; set; }
}
