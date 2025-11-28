using AsyncTaskOrchestrator.RabbitMq.Extensions;
using AsyncTaskOrchestrator.Services;
using AsyncTaskOrchestrator.WorkflowRunner;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMQ(builder.Configuration);
builder.Services.AddWorkflowOrchestrator();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();
