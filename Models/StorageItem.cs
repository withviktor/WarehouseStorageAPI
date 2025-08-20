using System.ComponentModel.DataAnnotations;

namespace WarehouseStorageAPI.Models;

public class StorageItem
{
    public int Id { get; set; }
        
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
        
    [Required]
    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;
        
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
        
    [Required]
    [StringLength(50)]
    public string Location { get; set; } = string.Empty;
        
    [Range(1, 100)]
    public int LedZone { get; set; }
        
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}