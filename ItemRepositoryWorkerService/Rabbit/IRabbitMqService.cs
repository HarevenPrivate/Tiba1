using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;

public interface IRabbitMqService
{
    Task PublishAsync(string queue, object message, CancellationToken cancellationToken = default);
    Task SubscribeAsync<T>(string queue, Func<T, Task> handler,ushort prefetchCount = 10,int consumerCount = 4, CancellationToken cancellationToken = default);

    Task StopAllSubscribersAsync();
}