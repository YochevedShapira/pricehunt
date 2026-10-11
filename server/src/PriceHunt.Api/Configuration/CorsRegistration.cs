namespace PriceHunt.Api.Configuration;

/// <summary>
/// Allows the browser app to call the API from its own origin. The origins
/// come from the <c>Cors:AllowedOrigins</c> setting; nothing is allowed by
/// wildcard.
/// </summary>
public static class CorsRegistration
{
    private const string SECTION_NAME = "Cors";
    private const string ALLOWED_ORIGINS_KEY = "AllowedOrigins";

    public static IServiceCollection AddBrowserClientCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var origins = configuration
            .GetSection(SECTION_NAME)
            .GetSection(ALLOWED_ORIGINS_KEY)
            .Get<string[]>() ?? [];

        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
