using System.ComponentModel.DataAnnotations;

namespace AsyncTaskOrchestrator.Services.Database.Options;

public class DatabaseCreateOptions
{
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public required string RequestedBy { get; set; }
}
