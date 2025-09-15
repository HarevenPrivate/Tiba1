using IteamRepositoryAPI.DTO;
namespace IteamRepositoryAPI.Services;

public interface IUserService
{
    Task<UserData?> GetUser(string userName);
    Task<(bool Result, string? ErrorDescription)> Register(string userName, string email, string password, string role = "User");
    Task<UserData?> Authenticate(string userName, string password);
    Task<bool> UpgradeToAdmin(Guid userId);

    
}
    


