using IteamRepository.Models;
using IteamRepositoryAPI.DTO;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
namespace IteamRepositoryAPI.Services;
public class ItemService : IItemService
{
    private IRabbitMqService _rabbit;
    ConcurrentDictionary<Guid, TaskCompletionSource<string>> _pending;

    public ItemService(IRabbitMqService rapit, ConcurrentDictionary<Guid, TaskCompletionSource<string>> pending)
    {
        _rabbit = rapit;
        _pending = pending;
    }
    public async Task<bool>  AddItem(string userId, string itemName)
    {
        Guid requestId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        var payload = JsonSerializer.Serialize(new
        {
            UserId = Guid.Parse(userId),
            ItemName = itemName
        });

        var req = new WorkRequest(
            requestId,
            WorkOperation.AddItem,
            payload
        );

        await _rabbit.PublishAsync("request", req);
        var completedTask = await Task.WhenAny( tcs.Task,Task.Delay(TimeSpan.FromSeconds(5)));

        // cleanup dictionary
        _pending.TryRemove(requestId, out _);

        if (completedTask == tcs.Task)
        {
            var responseJson = await tcs.Task;
            var response =  JsonSerializer.Deserialize<DetailWorkerResponse<Guid>>(responseJson);
            return response is not null && response.Success;
        }

        throw new TimeoutException("No response from worker within 5 seconds");
    }

    public async Task<IEnumerable<ItemData>> GetAllUserItems(string userId)
    {
        Guid requestId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        var payload = JsonSerializer.Serialize(new
        {
            UserId = Guid.Parse(userId)
        });

        var req = new WorkRequest(
            requestId,
            WorkOperation.GetAllUserItems,
            payload
        );

        await _rabbit.PublishAsync("request", req);

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        // cleanup dictionary
        _pending.TryRemove(requestId, out _);

        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("No response from worker within 5 seconds");
        }

        // worker responded → parse JSON payload
        var responseJson = await tcs.Task;

        var response = JsonSerializer.Deserialize<DetailWorkerResponse<List<ItemData>>>(responseJson);

        if (response is not null && response.Success && response.Resulte is not null)
        {
            return response.Resulte;
        }
        return Enumerable.Empty<ItemData>();

    }

    public async Task<bool> SoftDeleteItem(Guid itemId)
    {
        Guid requestId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        var payload = JsonSerializer.Serialize(new
        {
            ItemId = itemId
        });

        var req = new WorkRequest(
            requestId,
            WorkOperation.DeleteItem,
            payload
        );
        
        await _rabbit.PublishAsync("request", req);
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        // cleanup dictionary
        _pending.TryRemove(requestId, out _);

        if (completedTask == tcs.Task)
        {
            var responseJson = await tcs.Task;
            var response = JsonSerializer.Deserialize<DetailWorkerResponse<object>>(responseJson);
            
            return response?.Success ?? false;
        }

        throw new TimeoutException("No response from worker within 5 seconds");
    }

}
