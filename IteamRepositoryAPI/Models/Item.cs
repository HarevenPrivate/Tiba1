public class Item
{
    public Guid Id { get; set; } = Guid.NewGuid(); // GUID primary key
    public Guid UserId { get; set; } // Owner reference
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; } = false;
}
