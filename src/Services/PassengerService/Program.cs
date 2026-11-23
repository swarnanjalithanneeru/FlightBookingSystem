using FlightBookingSystem.Shared.Auth;
using FlightBookingSystem.Shared.Swagger;
using Microsoft.EntityFrameworkCore;
using PassengerService.Clients;
using PassengerService.Data;
using PassengerService.Repositories;
using PassengerService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Database
var connectionString = builder.Configuration.GetConnectionString("PassengerDbConnection");
builder.Services.AddDbContext<PassengerDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IPassengerRepository, PassengerRepository>();
builder.Services.AddScoped<ICheckInRepository, CheckInRepository>();

// HttpClients
var bookingServiceUrl = builder.Configuration["Services:BookingServiceUrl"] ?? "http://localhost:5003";
builder.Services.AddHttpClient<IBookingServiceClient, BookingServiceClient>(client =>
{
    client.BaseAddress = new Uri(bookingServiceUrl);
});

// Business Services
builder.Services.AddScoped<IPassengerService, PassengerServiceImpl>();
builder.Services.AddScoped<ICheckInService, CheckInServiceImpl>();

// JWT & Authorization
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// Swagger
builder.Services.AddSharedSwaggerGen("FlightBookingSystem - Passenger Service", "v1");

var app = builder.Build();

// Auto-migrate or ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PassengerDbContext>();
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Passenger Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
