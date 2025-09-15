
using IteamRepositoryAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Collections.Concurrent;
using System.Security.Cryptography.Xml;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetValue<string>("RabbitMq:ConnectionString");
if(string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("RabbitMQ connection string is not configured in appsettings.json");
builder.Services.AddRabbitMq(connectionString);
builder.Services.AddSingleton<ConcurrentDictionary<Guid, TaskCompletionSource<string>>>();
builder.Services.AddSingleton<ResponseSubscriber>();

builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IItemService, ItemService>();

// Add services to the container.
builder.Services.AddControllers();

// Add JWT authentication
//var key = Encoding.ASCII.GetBytes("YourSuperSecretKeyHere"); // keep it safe, later move to appsettings

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings["Key"]
                ?? throw new InvalidOperationException("JWT key is not configured in appsettings.json");
var key = Encoding.ASCII.GetBytes(keyString);


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "IteamRepository API", Version = "v1" });
    // Add JWT Authentication to Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,   // MUST be lowercase
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}"
    };
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            []
        }
    };
    c.AddSecurityRequirement(securityRequirement);

});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IteamRepository API V1");
        c.RoutePrefix = string.Empty;

        // Inject JS to automatically send JWT
        c.HeadContent = @"
        <script>
        (function () {
            const oldFetch = window.fetch;
            window.fetch = async function () {
                const token = localStorage.getItem('jwtToken');
                if (token) {
                    if (!arguments[1]) arguments[1] = {};
                    if (!arguments[1].headers) arguments[1].headers = {};
                    arguments[1].headers['Authorization'] = 'Bearer ' + token;
                }
                return oldFetch.apply(this, arguments);
            };
        })();
        </script>
        ";
    });
}



app.UseHttpsRedirection();

// **Authentication and Authorization middleware**
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// after building but before running
using (var scope = app.Services.CreateScope())
{
    var subscriber = scope.ServiceProvider.GetRequiredService<ResponseSubscriber>();
    await subscriber.StartAsync(app.Lifetime.ApplicationStopped); // start listening
}

app.Run();
