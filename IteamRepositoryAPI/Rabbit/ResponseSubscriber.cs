using IteamRepositoryAPI.DTO;
using System.Collections.Concurrent;

public class ResponseSubscriber
{
    private readonly IRabbitMqService _rabbit;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<string>> _pending;

    public ResponseSubscriber(IRabbitMqService rabbit,
                              ConcurrentDictionary<Guid, TaskCompletionSource<string>> pending)
    {
        _rabbit = rabbit;
        _pending = pending;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        await _rabbit.SubscribeAsync<WorkResponse>("response", async (response) =>
        {
            if (response is null) return;

            if (_pending.TryRemove(response.CorrelationId, out var tcs))
            {
                tcs.TrySetResult(response.Payload);
            }

            await Task.CompletedTask;
        },10,4, stoppingToken);

        stoppingToken.Register(async () => await _rabbit.StopAllSubscribersAsync());
    }
}