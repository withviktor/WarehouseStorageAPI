using WarehouseStorageAPI.Data;

namespace WarehouseStorageAPI.Seeds;

public class DatabaseSeeder
{
    private readonly WarehouseContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseSeeder(
        WarehouseContext context, 
        ILogger<DatabaseSeeder> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Get all seeder services
            var seeders = _serviceProvider.GetServices<ISeedData>();

            // Run all seeders
            foreach (var seeder in seeders)
            {
                _logger.LogInformation("Running seeder: {SeederType}", seeder.GetType().Name);
                await seeder.SeedAsync();
            }

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}