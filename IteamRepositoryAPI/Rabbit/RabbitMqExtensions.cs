using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

public static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, string connectionString)
    {


        services.AddSingleton(new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        });

        // you can also register a wrapper interface if you want
        services.AddSingleton<IRabbitMqService, RabbitMqService>();

        return services;
    }
}