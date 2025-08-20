using Microsoft.EntityFrameworkCore;
using WarehouseStorageAPI.Data;
using WarehouseStorageAPI.Models;

namespace WarehouseStorageAPI.Seeds;

public class StorageItemSeeder : ISeedData
{
    private readonly WarehouseContext _context;
    private readonly ILogger<StorageItemSeeder> _logger;

    public StorageItemSeeder(WarehouseContext context, ILogger<StorageItemSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedStorageItemsAsync();
    }

    private async Task SeedStorageItemsAsync()
    {
        // Check if any storage items already exist
        if (await _context.StorageItems.AnyAsync())
        {
            _logger.LogInformation("Storage items already exist, skipping seeding");
            return;
        }

        var sampleItems = GetSampleStorageItems();

        _context.StorageItems.AddRange(sampleItems);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} initial storage items", sampleItems.Count);
    }

    private static List<StorageItem> GetSampleStorageItems()
    {
        return new List<StorageItem>
        {
            new StorageItem
            {
                Name = "Arduino Uno R3",
                SKU = "ELEC001",
                Quantity = 150,
                Location = "A1-01",
                LedZone = 1,
                Description = "Microcontroller board based on ATmega328P",
                Price = 29.99m,
                Category = "Electronics",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            },
            new StorageItem
            {
                Name = "Raspberry Pi 4 Model B",
                SKU = "ELEC002",
                Quantity = 75,
                Location = "A1-02",
                LedZone = 1,
                Description = "Single-board computer with ARM Cortex-A72",
                Price = 89.99m,
                Category = "Electronics",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            },
            new StorageItem
            {
                Name = "Office Paper A4",
                SKU = "OFF001",
                Quantity = 500,
                Location = "B2-15",
                LedZone = 2,
                Description = "White A4 printing paper, 80gsm",
                Price = 12.50m,
                Category = "Office Supplies",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            },
            new StorageItem
            {
                Name = "Ballpoint Pens (Pack of 10)",
                SKU = "OFF002",
                Quantity = 200,
                Location = "B2-16",
                LedZone = 2,
                Description = "Blue ink ballpoint pens",
                Price = 8.99m,
                Category = "Office Supplies",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            },
            new StorageItem
            {
                Name = "Cordless Drill",
                SKU = "TOOL001",
                Quantity = 25,
                Location = "C3-08",
                LedZone = 3,
                Description = "18V cordless drill with battery pack",
                Price = 125.00m,
                Category = "Tools",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            },
            new StorageItem
            {
                Name = "Screwdriver Set",
                SKU = "TOOL002",
                Quantity = 40,
                Location = "C3-09",
                LedZone = 3,
                Description = "Professional screwdriver set with 12 pieces",
                Price = 35.99m,
                Category = "Tools",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            },
            new StorageItem
            {
                Name = "Safety Helmets",
                SKU = "SAFE001",
                Quantity = 100,
                Location = "D4-01",
                LedZone = 4,
                Description = "Industrial safety helmets, ANSI compliant",
                Price = 24.99m,
                Category = "Safety Equipment",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            },
            new StorageItem
            {
                Name = "LED Light Strips",
                SKU = "ELEC003",
                Quantity = 80,
                Location = "A1-03",
                LedZone = 1,
                Description = "RGB LED light strips, 5 meters",
                Price = 19.99m,
                Category = "Electronics",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            }
        };
    }
}