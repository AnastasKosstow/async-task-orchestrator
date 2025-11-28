using System.Collections.Concurrent;
using AsyncTaskOrchestrator.Common.Models;

namespace AsyncTaskOrchestrator.Services.Workflows.Repositories;

internal class WorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<Guid, Workflow> workflows = new();

    public Task CreateAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        workflows[workflow.Id] = workflow;
        return Task.CompletedTask;
    }

    public Task<Workflow?> GetAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        workflows.TryGetValue(workflowId, out var workflow);
        return Task.FromResult(workflow);
    }

    public Task SaveAsync(Workflow workflow, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        workflows[workflow.Id] = workflow;
        return Task.CompletedTask;
    }
}
