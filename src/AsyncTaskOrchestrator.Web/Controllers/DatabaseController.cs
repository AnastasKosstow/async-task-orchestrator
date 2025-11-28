using AsyncTaskOrchestrator.Services.Database.Options;
using AsyncTaskOrchestrator.Services.Database.Workflows;
using AsyncTaskOrchestrator.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace AsyncTaskOrchestrator.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController(
    IDatabaseCreationWorkflow creator
) : ControllerBase
{
    private readonly IDatabaseCreationWorkflow workflowCreator = creator;

    [HttpPost("workflows")]
    public async Task<ActionResult<WorkflowResponse>> CreateWorkflowAsync(
        [FromBody] DatabaseCreateOptions options,
        CancellationToken cancellationToken
    )
    {
        var response = await workflowCreator.CreateDatabaseCreationWorkflowAsync(options, cancellationToken);
        return Accepted(response);
    }
}
