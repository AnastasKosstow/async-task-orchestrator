using AsyncTaskOrchestrator.RabbitMq.Extensions;
using AsyncTaskOrchestrator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddRabbitMQ(builder.Configuration);
builder.Services.AddWorkflowOrchestrator();

var app = builder.Build();
app.UseAuthorization();
app.MapControllers();
app.Run();
