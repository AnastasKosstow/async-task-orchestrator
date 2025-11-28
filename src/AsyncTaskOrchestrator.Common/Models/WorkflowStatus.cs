namespace AsyncTaskOrchestrator.Common.Models;

public enum WorkflowStatus
{
    Created = 0,
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4
}
