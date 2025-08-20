using System.ComponentModel.DataAnnotations;

namespace WarehouseStorageAPI.Models;

public class UserActivity
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;
    
    public int? EntityId { get; set; }
    
    public string? Details { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}