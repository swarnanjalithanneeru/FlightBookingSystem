using FlightBookingSystem.Shared.Auth;
using FlightBookingSystem.Shared.Swagger;
using BookingService.Clients;
using BookingService.Data;
using BookingService.Repositories;
using BookingService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Database
var connectionString = builder.Configuration.GetConnectionString("BookingDbConnection");
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

// HttpClients for service-to-service communication
var flightServiceUrl = builder.Configuration["Services:FlightServiceUrl"] ?? "http://localhost:5002";
var passengerServiceUrl = builder.Configuration["Services:PassengerServiceUrl"] ?? "http://localhost:5004";
var paymentServiceUrl = builder.Configuration["Services:PaymentServiceUrl"] ?? "http://localhost:5005";

builder.Services.AddHttpClient<IFlightServiceClient, FlightServiceClient>(client =>
{
    client.BaseAddress = new Uri(flightServiceUrl);
});

builder.Services.AddHttpClient<IPassengerServiceClient, PassengerServiceClient>(client =>
{
    client.BaseAddress = new Uri(passengerServiceUrl);
});

builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>(client =>
{
    client.BaseAddress = new Uri(paymentServiceUrl);
});

// Business Services
builder.Services.AddScoped<IBookingService, BookingServiceImpl>();

// JWT & Authorization
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// Swagger
builder.Services.AddSharedSwaggerGen("FlightBookingSystem - Booking Service", "v1");

var app = builder.Build();

// Auto-migrate or ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
