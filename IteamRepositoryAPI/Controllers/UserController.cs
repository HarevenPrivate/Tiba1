
using IteamRepositoryAPI.DTO;
using IteamRepositoryAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IteamRepository.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;

    public UserController(IUserService userService, IConfiguration configuration)
    {
        _userService = userService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {

        if (!InputValidation.UserNameValidation(request.UserName))
        {
            return BadRequest("Invalid user name. It should be 3-30 characters long and contain only letters, digits, and underscores.");
        }


        if (!InputValidation.EmailValidation(request.Email))
        {
            return BadRequest("Invalid email format.");
        }

        if (!InputValidation.PasswordValidation(request.Password))
        {
            return BadRequest("Invalid password. It should be 4-50 characters long and not contain spaces or special characters like ' \" ; < > &.");
        }

        var rgisterd = await _userService.Register(request.UserName, request.Email, request.Password);
        if (rgisterd.Result)
        {
            return Ok("User registered successfully");
        }
        else
        {
            return BadRequest(rgisterd.ErrorDescription);
        }
        
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {

        if (!InputValidation.UserNameValidation(request.UserName))
        {
            return BadRequest("Invalid user name. It should be 3-30 characters long and contain only letters, digits, and underscores.");
        }

        if (!InputValidation.PasswordValidation(request.Password))
        {
            return BadRequest("Invalid password. It should be 4-50 characters long and not contain spaces or special characters like ' \" ; < > &.");
        }

        var user = await _userService.Authenticate(request.UserName, request.Password);
        if (user == null) return Unauthorized();

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    [Authorize] // This enforces JWT authentication
    [HttpPost("UpgradeToAdmin")]
    public async Task<IActionResult> UpgradeToAdmin()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if(string.IsNullOrEmpty(userId))
            return Unauthorized();

        bool IsUpgarated = await _userService.UpgradeToAdmin(Guid.Parse(userId));
        var token = string.Empty;
        if (IsUpgarated)
        {
            UserData user = new UserData { Role = "Admin", Id = Guid.Parse(userId) };
            token = GenerateJwtToken(user);
            return Ok(new { token });
        }
        return BadRequest("UpgradeToAdmin FAIL");


    }

    private string GenerateJwtToken(UserData user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "SuperSecretKeyForJwt");

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    

    public record RegisterRequest(string UserName, string Email, string Password);
    public record LoginRequest(string UserName, string Password);
}
