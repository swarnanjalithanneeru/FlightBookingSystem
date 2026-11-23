using FlightBookingSystem.Shared.Auth;
using FlightBookingSystem.Shared.Swagger;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Repositories;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Database
var connectionString = builder.Configuration.GetConnectionString("UserDbConnection");
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();

// Business Services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserServiceImpl>();

// JWT & Authorization
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// Swagger
builder.Services.AddSharedSwaggerGen("FlightBookingSystem - User Service", "v1");

var app = builder.Build();

// Auto-migrate or ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
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

// Configure HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
