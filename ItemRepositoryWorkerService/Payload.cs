using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemRepositoryWorkerService
{
    public record AddItemPayload(Guid UserId, string ItemName);
    public record GetItemsPayload(Guid UserId);
    public record DeleteItemPayload(Guid ItemId);

    public record GetUserPayload(string UserName);
    
    public record RegisterUserPayload(string UserName, string Email, string PasswordHash, string Role);


    public record UpgradeToAdminPayload(Guid UserId);



}
