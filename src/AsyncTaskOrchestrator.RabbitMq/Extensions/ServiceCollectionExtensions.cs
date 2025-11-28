using AsyncTaskOrchestrator.RabbitMq.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AsyncTaskOrchestrator.RabbitMq.Extensions;

public static class ServiceCollectionExtensions
{
    private const string SECTION_NAME = "rabbitmq";

    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqSettings>()
            .Configure<IConfiguration>((settings, _) =>
            {
                configuration.GetSection(SECTION_NAME).Bind(settings);
            });

        services.AddSingleton<IRabbitMqConnection>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
            return new RabbitMqConnection(settings);
        });

        return services;
    }
}
