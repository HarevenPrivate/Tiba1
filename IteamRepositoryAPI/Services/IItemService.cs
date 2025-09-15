using IteamRepositoryAPI.DTO;

namespace IteamRepositoryAPI.Services
{
    public interface IItemService
    {
        Task<bool> AddItem(string userId, string itemName);
        Task<IEnumerable<ItemData>> GetAllUserItems(string userId);
        Task<bool> SoftDeleteItem(Guid itemId);
    }
}
