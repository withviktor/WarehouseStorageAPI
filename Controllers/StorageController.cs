using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseStorageAPI.Data;
using WarehouseStorageAPI.DTOs;
using WarehouseStorageAPI.Models;
using WarehouseStorageAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace WarehouseStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all storage operations
public class StorageController : ControllerBase
{
    private readonly WarehouseContext _context;
    private readonly ILedControllerService _ledService;
    private readonly ILogger<StorageController> _logger;
    private readonly IUserActionService _userActionService;

    public StorageController(
        WarehouseContext context,
        ILedControllerService ledService,
        ILogger<StorageController> logger,
        IUserActionService userActionService)
    {
        _context = context;
        _ledService = ledService;
        _logger = logger;
        _userActionService = userActionService;
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

        // Log user action
        await _userActionService.LogActionAsync(
            User,
            "Create Storage Item",
            $"Created item: {item.Name} (SKU: {item.SKU})",
            "StorageItem",
            item.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, UpdateStorageItemDto dto)
    {
        var item = await _context.StorageItems.FindAsync(id);
        if (item == null)
            return NotFound();

        var previousQuantity = item.Quantity;
        var changes = new List<string>();

        // Update fields if provided and track changes
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name != item.Name)
        {
            changes.Add($"Name: {item.Name} → {dto.Name}");
            item.Name = dto.Name;
        }
        if (dto.Quantity.HasValue && dto.Quantity.Value != item.Quantity)
        {
            changes.Add($"Quantity: {item.Quantity} → {dto.Quantity.Value}");
            item.Quantity = dto.Quantity.Value;
        }
        if (!string.IsNullOrEmpty(dto.Location) && dto.Location != item.Location)
        {
            changes.Add($"Location: {item.Location} → {dto.Location}");
            item.Location = dto.Location;
        }
        if (dto.LedZone.HasValue && dto.LedZone.Value != item.LedZone)
        {
            changes.Add($"LED Zone: {item.LedZone} → {dto.LedZone.Value}");
            item.LedZone = dto.LedZone.Value;
        }
        if (dto.Description != null && dto.Description != item.Description)
        {
            changes.Add($"Description updated");
            item.Description = dto.Description;
        }
        if (dto.Price.HasValue && dto.Price.Value != item.Price)
        {
            changes.Add($"Price: {item.Price} → {dto.Price.Value}");
            item.Price = dto.Price.Value;
        }
        if (dto.Category != null && dto.Category != item.Category)
        {
            changes.Add($"Category: {item.Category} → {dto.Category}");
            item.Category = dto.Category;
        }

        item.LastUpdated = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log quantity change if applicable
        if (dto.Quantity.HasValue && dto.Quantity.Value != previousQuantity)
        {
            var transactionType = dto.Quantity.Value > previousQuantity ? "IN" : "OUT";
            var quantityChange = Math.Abs(dto.Quantity.Value - previousQuantity);
            await LogTransaction(id, transactionType, quantityChange, previousQuantity, dto.Quantity.Value, "Manual adjustment");
        }

        // Log user action
        if (changes.Any())
        {
            await _userActionService.LogActionAsync(
                User,
                "Update Storage Item",
                $"Updated item {item.Name} (SKU: {item.SKU}): {string.Join(", ", changes)}",
                "StorageItem",
                item.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
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

        // Log user action
        await _userActionService.LogActionAsync(
            User,
            "Adjust Inventory",
            $"Adjusted inventory for {item.Name} (SKU: {item.SKU}): {dto.TransactionType} {Math.Abs(newQuantity - previousQuantity)} units. {dto.Notes}",
            "StorageItem",
            item.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

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

        // Log user action
        await _userActionService.LogActionAsync(
            User,
            "Highlight Item",
            $"Highlighted item {item.Name} (SKU: {item.SKU}) at location {item.Location} with color {color}",
            "StorageItem",
            item.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

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

        // Log user action
        await _userActionService.LogActionAsync(
            User,
            "Delete Storage Item",
            $"Deleted item {item.Name} (SKU: {item.SKU})",
            "StorageItem",
            item.Id,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

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