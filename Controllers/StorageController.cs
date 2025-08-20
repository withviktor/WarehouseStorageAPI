using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseStorageAPI.Data;
using WarehouseStorageAPI.DTOs;
using WarehouseStorageAPI.Models;
using WarehouseStorageAPI.Services;

namespace WarehouseStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StorageController : ControllerBase
{
    private readonly WarehouseContext _context;
    private readonly ILedControllerService _ledService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(
        WarehouseContext context,
        ILedControllerService ledService,
        ILogger<StorageController> logger)
    {
        _context = context;
        _ledService = ledService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StorageItem>>> GetItems(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool activeOnly = true)
    {
        var query = _context.StorageItems.AsQueryable();

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x => 
                x.Name.Contains(search) || 
                x.SKU.Contains(search) ||
                x.Location.Contains(search));
        }

        if (!string.IsNullOrEmpty(category))
            query = query.Where(x => x.Category == category);

        return await query.OrderBy(x => x.Location).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StorageItem>> GetItem(int id)
    {
        var item = await _context.StorageItems.FindAsync(id);
        if (item == null)
            return NotFound();

        return item;
    }

    [HttpPost]
    public async Task<ActionResult<StorageItem>> CreateItem(CreateStorageItemDto dto)
    {
        // Check if SKU already exists
        if (await _context.StorageItems.AnyAsync(x => x.SKU == dto.SKU))
        {
            return BadRequest("SKU already exists");
        }

        var item = new StorageItem
        {
            Name = dto.Name,
            SKU = dto.SKU,
            Quantity = dto.Quantity,
            Location = dto.Location,
            LedZone = dto.LedZone,
            Description = dto.Description,
            Price = dto.Price,
            Category = dto.Category
        };

        _context.StorageItems.Add(item);
        await _context.SaveChangesAsync();

        // Log transaction
        await LogTransaction(item.Id, "IN", dto.Quantity, 0, dto.Quantity, "Initial stock");

        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, UpdateStorageItemDto dto)
    {
        var item = await _context.StorageItems.FindAsync(id);
        if (item == null)
            return NotFound();

        var previousQuantity = item.Quantity;

        // Update fields if provided
        if (!string.IsNullOrEmpty(dto.Name))
            item.Name = dto.Name;
        if (dto.Quantity.HasValue)
            item.Quantity = dto.Quantity.Value;
        if (!string.IsNullOrEmpty(dto.Location))
            item.Location = dto.Location;
        if (dto.LedZone.HasValue)
            item.LedZone = dto.LedZone.Value;
        if (dto.Description != null)
            item.Description = dto.Description;
        if (dto.Price.HasValue)
            item.Price = dto.Price.Value;
        if (dto.Category != null)
            item.Category = dto.Category;

        item.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log quantity change if applicable
        if (dto.Quantity.HasValue && dto.Quantity.Value != previousQuantity)
        {
            var transactionType = dto.Quantity.Value > previousQuantity ? "IN" : "OUT";
            var quantityChange = Math.Abs(dto.Quantity.Value - previousQuantity);
            await LogTransaction(id, transactionType, quantityChange, previousQuantity, dto.Quantity.Value, "Manual adjustment");
        }

        return NoContent();
    }

    [HttpPost("{id}/adjust")]
    public async Task<IActionResult> AdjustInventory(int id, InventoryAdjustmentDto dto)
    {
        var item = await _context.StorageItems.FindAsync(id);
        if (item == null)
            return NotFound();

        var previousQuantity = item.Quantity;
        var newQuantity = dto.TransactionType.ToUpper() switch
        {
            "IN" => item.Quantity + dto.Quantity,
            "OUT" => Math.Max(0, item.Quantity - dto.Quantity),
            "ADJUST" => dto.Quantity,
            _ => item.Quantity
        };

        item.Quantity = newQuantity;
        item.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogTransaction(id, dto.TransactionType, Math.Abs(newQuantity - previousQuantity), previousQuantity, newQuantity, dto.Notes);

        return Ok(new { PreviousQuantity = previousQuantity, NewQuantity = newQuantity });
    }

    [HttpPost("{id}/highlight")]
    public async Task<IActionResult> HighlightItem(int id, [FromQuery] string color = "blue")
    {
        var item = await _context.StorageItems.FindAsync(id);
        if (item == null)
            return NotFound();

        var success = await _ledService.HighlightLocationAsync(item.Location, color);
        if (!success)
            return StatusCode(500, "Failed to control LED");

        _logger.LogInformation($"Highlighted item {item.SKU} at location {item.Location}");
        return Ok(new { Message = $"Highlighted {item.Name} at {item.Location}" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.StorageItems.FindAsync(id);
        if (item == null)
            return NotFound();

        item.IsActive = false;
        item.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var categories = await _context.StorageItems
            .Where(x => x.IsActive && !string.IsNullOrEmpty(x.Category))
            .Select(x => x.Category!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return categories;
    }

    [HttpGet("locations")]
    public async Task<ActionResult<IEnumerable<string>>> GetLocations()
    {
        var locations = await _context.StorageItems
            .Where(x => x.IsActive)
            .Select(x => x.Location)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return locations;
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<IEnumerable<InventoryTransaction>>> GetItemTransactions(int id)
    {
        var transactions = await _context.InventoryTransactions
            .Where(x => x.StorageItemId == id)
            .OrderByDescending(x => x.Timestamp)
            .Take(50)
            .ToListAsync();

        return transactions;
    }

    private async Task LogTransaction(int itemId, string type, int quantity, int previousQty, int newQty, string? notes)
    {
        var transaction = new InventoryTransaction
        {
            StorageItemId = itemId,
            TransactionType = type.ToUpper(),
            Quantity = quantity,
            PreviousQuantity = previousQty,
            NewQuantity = newQty,
            Notes = notes
        };

        _context.InventoryTransactions.Add(transaction);
        await _context.SaveChangesAsync();
    }
}