using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.WorkflowRunner.Exceptions;

public sealed class WorkflowHandlerNotRegisteredException(WorkflowType type, Exception innerException) 
    : Exception($"No workflow handler registered for workflow type '{type}'.", innerException)
{
    public WorkflowType Type { get; } = type;
}
