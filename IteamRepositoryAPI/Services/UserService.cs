using IteamRepository.Models;
using IteamRepositoryAPI.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;



namespace IteamRepositoryAPI.Services
{
    public class UserService : IUserService
    {
        private IRabbitMqService _rabbit;
        ConcurrentDictionary<Guid, TaskCompletionSource<string>> _pending;

        public UserService(IRabbitMqService rabbit, ConcurrentDictionary<Guid, TaskCompletionSource<string>> pending)
        {
            _rabbit = rabbit;
            _pending = pending;
        }

        public async Task<UserData?> GetUser(string userName)
        {
            Guid requestId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[requestId] = tcs;

            var payload = JsonSerializer.Serialize(new
            {
                UserName = userName
            });

            var req = new WorkRequest(
                requestId,
                WorkOperation.GetUser,
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

            var responce = JsonSerializer.Deserialize<DetailWorkerResponse<UserData>>(responseJson)
                   ?? null;

            if (responce is not null && responce.Success)
            {
                return responce.Resulte;
            }

            return null;
        }

        public async Task<(bool Result, string? ErrorDescription)> Register(string userName, string email, string password, string role = "User")
        {
            var user = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = role
            };


            Guid requestId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[requestId] = tcs;

            var payload = JsonSerializer.Serialize(new
            {
                UserName = userName,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = role
            });

            var req = new WorkRequest(
                requestId,
                WorkOperation.RegisterUser,
                payload
            );

            await _rabbit.PublishAsync("request", req);

            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            // cleanup dictionary
            _pending.TryRemove(requestId, out _);

            if (completedTask == tcs.Task)
            {
                // worker responded → parse JSON payload
                var responseJson = await tcs.Task;

                var response = JsonSerializer.Deserialize<DetailWorkerResponse<object>>(responseJson);

                if (response == null)
                    return (false, "Invalid response from worker");
                if (!response.Success)
                    return (false, response.Error ?? "Unknown error from worker");

                return (true, null);
            }

            throw new TimeoutException("No response from worker within 5 seconds");

        }

        

        public async Task<UserData?> Authenticate(string userName, string password)
        {
            var currentUser = await GetUser(userName);
            if (null != currentUser)
            {
                if (VerifyPassword(password, currentUser.PasswordHash))
                    return currentUser;
            }
            return null;
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            var passwordHash = HashPassword(password);
            return passwordHash == hash;
        }

        public async Task<bool> UpgradeToAdmin(Guid userId)
        {

            Guid requestId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[requestId] = tcs;

            var payload = JsonSerializer.Serialize(new
            {
                UserId = userId
            });

            var req = new WorkRequest(
                requestId,
                WorkOperation.UpgradeToAdmin,
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

            var responce = JsonSerializer.Deserialize<DetailWorkerResponse<object>>(responseJson)
                   ?? null;

            if (responce is not null && responce.Success)
            {
                return responce.Success;
            }

            return false;
        }
    }
}
