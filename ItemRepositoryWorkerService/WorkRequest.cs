namespace IteamRepositoryAPI.DTO
{
    public enum WorkOperation
    {
        RegisterUser,
        UserLogin,
        AddItem,
        DeleteItem,
        GetAllUserItems,
        GetUser,
        UpgradeToAdmin
    };
    public record WorkRequest(Guid CorrelationId, WorkOperation Operation, string Payload);
    public record WorkResponse(Guid CorrelationId, string Payload);

    public class DetailWorkerResponse<T>
    {
        public bool Success { get; set; }
        public T? Resulte { get; set; }
        public string? Error { get; set; } // optional, in case you return errors
    }

    public class UserData
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = "User";
        public string PasswordHash { get; set; } = null!;
    }

    public class ItemData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
