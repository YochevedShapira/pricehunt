using Microsoft.Extensions.Options;
using PriceHunt.Application.Searching;

namespace PriceHunt.Api.Configuration;

/// <summary>
/// Binds <see cref="SearchOptions"/> from the <c>Search</c> configuration
/// section. This is the only place that knows the section exists: the
/// application layer receives the bound object, not <see cref="IOptions{T}"/>.
/// </summary>
public static class SearchOptionsRegistration
{
    private const string SECTION_NAME = "Search";

    public static IServiceCollection AddSearchOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<SearchOptions>()
            .Bind(configuration.GetSection(SECTION_NAME))
            .Validate(
                options => options.Timeout > TimeSpan.Zero,
                $"{SECTION_NAME}:{nameof(SearchOptions.Timeout)} must be a positive duration.")
            .ValidateOnStart();

        services.AddSingleton(provider =>
            provider.GetRequiredService<IOptions<SearchOptions>>().Value);

        return services;
    }
}
