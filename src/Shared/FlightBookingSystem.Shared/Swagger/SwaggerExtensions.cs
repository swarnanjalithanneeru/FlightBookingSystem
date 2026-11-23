using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace FlightBookingSystem.Shared.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSharedSwaggerGen(this IServiceCollection services, string apiTitle, string apiVersion = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(apiVersion, new OpenApiInfo
            {
                Title = apiTitle,
                Version = apiVersion,
                Description = $"{apiTitle} - Flight Booking System Microservice API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' [space] and your valid JWT token.\n\nExample: \"Bearer eyJhbGciOi...\""
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
