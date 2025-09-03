using Microsoft.EntityFrameworkCore;
using TodoAPI.Data;
using System.Data;
using Npgsql;
using TodoAPI.Service;
using TodoAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавляем DbContext с PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetSection("Redis")["Connection"];
});
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
// Контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Добавляем логирование
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();
app.UseGrpcWeb();
app.MapGrpcService<TodoAnalyticsService>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var connectionString = builder.Configuration.GetConnectionString("Postgres");

    int maxRetries = 30;
    int delaySeconds = 2;

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            db.Database.Migrate();
            app.Logger.LogInformation("Database migrated successfully");
            break;
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning($"DB not ready (attempt {i + 1}/{maxRetries}): {ex.Message}");
            if (i == maxRetries - 1)
                throw;
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
}


// Swagger конфигурация
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API V1");
        c.RoutePrefix = ""; // Swagger на корне
    });
    app.MapGrpcReflectionService();
}


app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Time = DateTime.UtcNow }));

app.Logger.LogInformation("Todo API starting...");
app.Run();