using ItemRepositoryWorkerService.DBHandler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    // EF Core DbContext
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(hostContext.Configuration.GetConnectionString("Postgres")));
    var connectionString = hostContext.Configuration.GetValue<string>("RabbitMq:ConnectionString");
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("RabbitMQ connection string is not configured in appsettings.json");
    services.AddRabbitMq(connectionString);

    
    // Your worker
    services.AddHostedService<ItemRepositoryWorker>();
});



var host = builder.Build();

// Run EF migrations automatically
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

await host.RunAsync();
