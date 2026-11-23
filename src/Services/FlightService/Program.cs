using FlightBookingSystem.Shared.Auth;
using FlightBookingSystem.Shared.Swagger;
using FlightService.Data;
using FlightService.Repositories;
using FlightService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Database
var connectionString = builder.Configuration.GetConnectionString("FlightDbConnection");
builder.Services.AddDbContext<FlightDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IFlightRepository, FlightRepository>();
builder.Services.AddScoped<IFlightSeatRepository, FlightSeatRepository>();

// Business Services
builder.Services.AddScoped<IFlightService, FlightServiceImpl>();

// JWT & Authorization
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// Swagger
builder.Services.AddSharedSwaggerGen("FlightBookingSystem - Flight Service", "v1");

var app = builder.Build();

// Auto-migrate or ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlightDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not apply migrations automatically during startup. Fallback to EnsureCreated.");
        try
        {
            db.Database.EnsureCreated();
        }
        catch (Exception ensureEx)
        {
            logger.LogWarning(ensureEx, "Could not ensure database created.");
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flight Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
