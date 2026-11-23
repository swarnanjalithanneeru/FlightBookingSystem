using FlightBookingSystem.Shared.Auth;
using FlightBookingSystem.Shared.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// JWT Authentication
builder.Services.AddSharedJwtAuthentication(builder.Configuration);

// Swagger
builder.Services.AddSharedSwaggerGen("FlightBookingSystem - API Gateway", "v1");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Unified Swagger UI for API Gateway
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway");
    c.SwaggerEndpoint("http://localhost:5001/swagger/v1/swagger.json", "User Service");
    c.SwaggerEndpoint("http://localhost:5002/swagger/v1/swagger.json", "Flight Service");
    c.SwaggerEndpoint("http://localhost:5003/swagger/v1/swagger.json", "Booking Service");
    c.SwaggerEndpoint("http://localhost:5004/swagger/v1/swagger.json", "Passenger Service");
    c.SwaggerEndpoint("http://localhost:5005/swagger/v1/swagger.json", "Payment Service");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map YARP reverse proxy endpoints
app.MapReverseProxy();

app.Run();
