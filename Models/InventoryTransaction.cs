namespace WarehouseStorageAPI.Models;

public class InventoryTransaction
{
    public int Id { get; set; }
    public int StorageItemId { get; set; }
    public StorageItem StorageItem { get; set; } = null!;
    public string TransactionType { get; set; } = string.Empty; // "IN", "OUT", "ADJUST"
    public int Quantity { get; set; }
    public int PreviousQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? UserId { get; set; }
}