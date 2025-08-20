using Microsoft.EntityFrameworkCore;
using WarehouseStorageAPI.Data;
using WarehouseStorageAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<WarehouseContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Data Source=warehouse.db"));

// Services
builder.Services.AddSingleton<ILedControllerService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<LedControllerService>>();
    var env = provider.GetRequiredService<IWebHostEnvironment>();
    var config = provider.GetRequiredService<IConfiguration>();
    return new LedControllerService(logger, env, config);
});

// CORS for React Native
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WarehouseContext>();
    context.Database.EnsureCreated();
}

app.Run();