using Microsoft.EntityFrameworkCore;
using WarehouseStorageAPI.Models;

namespace WarehouseStorageAPI.Data;


public class WarehouseContext : DbContext
{
    public WarehouseContext(DbContextOptions<WarehouseContext> options) 
        : base(options) { }

    public DbSet<StorageItem> StorageItems { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure StorageItem
        modelBuilder.Entity<StorageItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Location);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // Configure InventoryTransaction
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.StorageItem)
                  .WithMany()
                  .HasForeignKey(e => e.StorageItemId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data with static dates
        modelBuilder.Entity<StorageItem>().HasData(
            new StorageItem
            {
                Id = 1,
                Name = "Sample Item",
                SKU = "SAMPLE001",
                Quantity = 100,
                Location = "A1-01",
                LedZone = 1,
                Description = "Sample warehouse item",
                Price = 29.99m,
                Category = "Electronics",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new StorageItem
            {
                Id = 2,
                Name = "Test Widget",
                SKU = "WIDGET001",
                Quantity = 50,
                Location = "A2-01",
                LedZone = 2,
                Description = "Test widget for demo",
                Price = 15.50m,
                Category = "Hardware",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        );
    }
}