using IteamRepositoryAPI.DTO;
using ItemRepositoryWorkerService;
using ItemRepositoryWorkerService.DBHandler;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;

public class ItemRepositoryWorker : BackgroundService
{
    private readonly IRabbitMqService _rabbit;
    private readonly IServiceScopeFactory _scopeFactory; // For EF DbContext
    private const string RequestQueue = "request";
    private const string ResponseQueue = "response";
    private readonly ILogger<ItemRepositoryWorker> _logger;

    public ItemRepositoryWorker(IRabbitMqService rabbit, IServiceScopeFactory scopeFactory, ILogger<ItemRepositoryWorker> logger)
    {
        _rabbit = rabbit;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Subscribe to request queue
        await _rabbit.SubscribeAsync<WorkRequest>(RequestQueue, HandleRequestAsync,10,4, cancellationToken);

        cancellationToken.Register(async () => await _rabbit.StopAllSubscribersAsync());
    }

    private async Task HandleRequestAsync(WorkRequest request)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        object payloadResult;

        switch (request.Operation)
        {
            case WorkOperation.AddItem:
                payloadResult = await AddItemAsync(db, request.Payload, request.CorrelationId);
                break;

            case WorkOperation.GetAllUserItems:
                payloadResult = await GetAllUserItemsAsync(db, request.Payload);
                break;

            case WorkOperation.DeleteItem:
                payloadResult = await SoftDeleteItemAsync(db, request.Payload);
                break;
            case WorkOperation.GetUser:
                payloadResult = await GetUserAsync(db, request.Payload);
                break;
            case WorkOperation.RegisterUser:
                payloadResult = await RegisterUserAsync(db, request.Payload, request.CorrelationId);
                break;

            case WorkOperation.UpgradeToAdmin:
                payloadResult = await UpgradeToAdminAsync(db, request.Payload);
                break;

            default:
                payloadResult = new { Error = "Unknown operation" };
                break;
        }

        // Send response to response queue with same CorrelationId
        var response = new WorkResponse(request.CorrelationId, JsonSerializer.Serialize(payloadResult));
        

        await _rabbit.PublishAsync(ResponseQueue, response);
    }

    private async Task<object> UpgradeToAdminAsync(AppDbContext db, string payload)
    {
        var response = new DetailWorkerResponse<object>();
        var data = JsonSerializer.Deserialize<UpgradeToAdminPayload>(payload)!;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == data.UserId);
        if (user is null)
        {
            response.Success = false;
            response.Error = $"user id: {data.UserId} not exist";
        }
        else
        {
            user.Role = "Admin";
            await db.SaveChangesAsync();
            response.Success = true;
        }

        return response;
    }

    private async Task<object> RegisterUserAsync(AppDbContext db, string payload, Guid correlationId)
    {
        var response = new DetailWorkerResponse<object>();
        var data = JsonSerializer.Deserialize<RegisterUserPayload>(payload)!;
        User newUser = new User
        {
            Id = correlationId,
            UserName = data.UserName,
            Email = data.Email,
            PasswordHash = data.PasswordHash,
            Role = data.Role
        };

        
        try
        {
            await db.Users.AddAsync(newUser);
            await db.SaveChangesAsync();
            response.Success = true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Handle unique violations
            if (pgEx.ConstraintName != null)
            {
                if (pgEx.ConstraintName.Contains("UserName"))
                {
                    response.Success = false;
                    response.Error = $"Username already exists: {data.UserName}";
                }
                else if (pgEx.ConstraintName.Contains("PK_Users"))
                {
                    response.Success = true;
                    response.Error = $"User with ID {correlationId} already exists rabbit isuue duplicate message";
                }
                else
                {
                    response.Success = false;
                    response.Error = "Unique constraint violation on Users table.";
                }
            }
            else
            {
                response.Success = false;
                response.Error = "Unique constraint violation, but constraint name unknown.";
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Error = ex.Message;
        }

        return response;

    }

    private async Task<object> GetUserAsync(AppDbContext db, string payload)
    {
        var response = new DetailWorkerResponse<UserData>();
        var data = JsonSerializer.Deserialize<GetUserPayload>(payload)!;

        var user = await db.Users.FirstOrDefaultAsync(x => x.UserName == data.UserName);
        if (user is null)
        {
            response.Error = $"user name not exist {data.UserName}";
            response.Success = false;
            return response;
        }
        
        UserData userData = new UserData
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Role = user.Role,
            PasswordHash = user.PasswordHash
        };

        response.Success = true;
        response.Resulte = userData;
        return response;
        
    }

    private async Task<object> AddItemAsync(AppDbContext db, string payloadJson, Guid correlationId)
    {
        var response = new DetailWorkerResponse<Guid>();
        
        var data = JsonSerializer.Deserialize<AddItemPayload>(payloadJson)!;

        var item = new Item
        {
            Id = correlationId,
            UserId = data.UserId,
            Name = data.ItemName,
            IsDeleted = false,
        };

        try
        {
            await db.Items.AddAsync(item);
            await db.SaveChangesAsync();

            response.Success = true;
            response.Resulte = correlationId;
;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Unique violation → item already exists, treat as success
            // in case duplicate message that can happen in case of using rabbit but this is not happen a lot this is the reason
            // that it is better to user exception in this case it is better then checking if the item exist before adding it
            // In the user table the username must be unique so this exception can be thrown and duplicate username the client shoudl handle it
            response.Success = true;
            response.Resulte = correlationId;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Error = ex.Message;
        }

        return response;
    }

    private async Task<object> GetAllUserItemsAsync(AppDbContext db, string payloadJson)
    {
        var response = new DetailWorkerResponse<List<ItemData>>();
        var data = JsonSerializer.Deserialize<GetItemsPayload>(payloadJson)!;

        var items = await db.Items
            .Where(x => x.UserId == data.UserId && !x.IsDeleted)
            .Select(i => new ItemData
            {
                Id = i.Id,
                Name = i.Name
            })
            .ToListAsync();

        response.Success = true;
        response.Resulte = items == null ? new List<ItemData>() : items;

        return response;
    }

    private async Task<object> SoftDeleteItemAsync(AppDbContext db, string payloadJson)
    {
        var response = new DetailWorkerResponse<object>();
        var data = JsonSerializer.Deserialize<DeleteItemPayload>(payloadJson)!;

        var item = await db.Items.FirstOrDefaultAsync(x => x.Id == data.ItemId);
        if (item is null)
        {
            response.Success = false;
            response.Error = $"Item id: {data.ItemId} not exist";
        }
        else
        {
            item.IsDeleted = true;
            await db.SaveChangesAsync();
            response.Success = true;
        }

        return response;
    }

    
}
