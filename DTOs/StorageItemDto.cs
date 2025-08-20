namespace WarehouseStorageAPI.DTOs;

public class CreateStorageItemDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Location { get; set; } = string.Empty;
    public int LedZone { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
}

public class UpdateStorageItemDto
{
    public string? Name { get; set; }
    public int? Quantity { get; set; }
    public string? Location { get; set; }
    public int? LedZone { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Category { get; set; }
}

public class InventoryAdjustmentDto
{
    public int Quantity { get; set; }
    public string TransactionType { get; set; } = "ADJUST";
    public string? Notes { get; set; }
}