namespace IteamRepository.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid(); // GUID primary key
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User"; // Default role is User
}
