namespace AsyncTaskOrchestrator.Common.Execution;

public record class WorkflowResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static WorkflowResult Success(string message = "") => new()
    {
        Succeeded = true,
        Message = message
    };

    public static WorkflowResult Failure(string message) => new()
    {
        Succeeded = false,
        Message = message
    };
}

