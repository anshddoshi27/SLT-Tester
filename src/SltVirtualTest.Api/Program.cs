using Microsoft.EntityFrameworkCore;
using SltVirtualTest.Api.Data;
using SltVirtualTest.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dbDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "SltVirtualTest");
Directory.CreateDirectory(dbDirectory);
var dbPath = Path.Combine(dbDirectory, "sltvirtualtest.db");
var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = string.IsNullOrWhiteSpace(configuredConnection)
    ? $"Data Source={dbPath}"
    : configuredConnection;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TestExecutorService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "https://localhost:7139",
                "http://localhost:5295",
                "https://localhost:7107",
                "http://localhost:5215")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    app.Logger.LogInformation("SQLite database: {ConnectionString}", connectionString);
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Failed to create SQLite database at {DbPath}. Free disk space on your Mac and try again.", dbPath);
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();
app.Run();
