namespace IteamRepositoryAPI.DTO
{
    public record AddItemRequest(string Name);
    public record DeleteItemRequest(Guid itemId);

}
