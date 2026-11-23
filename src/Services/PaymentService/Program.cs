using FlightBookingSystem.Shared.Auth;
using FlightBookingSystem.Shared.Swagger;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Repositories;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Database
var connectionString = builder.Configuration.GetConnectionString("PaymentDbConnection");
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Business Services
builder.Services.AddScoped<IPaymentService, PaymentServiceImpl>();

// JWT & Authorization
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// Swagger
builder.Services.AddSharedSwaggerGen("FlightBookingSystem - Payment Service", "v1");

var app = builder.Build();

// Auto-migrate or ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
