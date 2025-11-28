using AsyncTaskOrchestrator.Common.Models;
using AsyncTaskOrchestrator.Services.Database.Handlers;
using AsyncTaskOrchestrator.Services.Database.Options;
using AsyncTaskOrchestrator.Services.Database.Workflows;
using AsyncTaskOrchestrator.Services.Workflows;
using AsyncTaskOrchestrator.Services.Workflows.Factories;
using AsyncTaskOrchestrator.Services.Workflows.Repositories;
using AsyncTaskOrchestrator.Services.Workflows.Submitters;
using AsyncTaskOrchestrator.Services.Workflows.Submitters.Publisher;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncTaskOrchestrator.Services;

public static class WorkflowsExtensions
{
    public static IServiceCollection AddWorkflowOrchestrator(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowRepository, WorkflowRepository>();
        services.AddSingleton<IQueuePublisher, QueuePublisher>();

        services.AddDatabaseWorkflow();
        services.AddHandlers();
        services.AddFactories();

        return services;
    }

    public static IServiceCollection AddDatabaseWorkflow(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowSubmitter<DatabaseCreateOptions>, WorkflowSubmitter<DatabaseCreateOptions>>();
        services.AddScoped<IDatabaseCreationWorkflow, DatabaseCreationWorkflow>();

        return services;
    }

    public static IServiceCollection AddHandlers(this IServiceCollection services)
    {
        services.AddKeyedScoped<IWorkflowHandler, CreateDatabaseHandler>(WorkflowType.DatabaseCreation);

        return services;
    }

    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        services.AddSingleton<IWorkflowFactory<DatabaseCreateOptions>, WorkflowFactory<DatabaseCreateOptions>>();

        return services;
    }
}
