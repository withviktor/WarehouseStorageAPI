using System.ComponentModel.DataAnnotations;

namespace WarehouseStorageAPI.Models;

public class UserAction
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty; // e.g., "Created Storage Item", "Updated Inventory", "LED Control"
    
    [StringLength(500)]
    public string? Details { get; set; } // Additional details about the action
    
    [StringLength(100)]
    public string? EntityType { get; set; } // e.g., "StorageItem", "LED"
    
    public int? EntityId { get; set; } // ID of the affected entity
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    // Navigation property
    public User User { get; set; } = null!;
}
