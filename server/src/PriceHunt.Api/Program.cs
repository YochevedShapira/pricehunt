using PriceHunt.Api.Configuration;
using PriceHunt.Api.History;
using PriceHunt.Api.Searching;
using PriceHunt.Api.Suppliers;
using PriceHunt.Application;
using PriceHunt.Infrastructure.Persistence;
using PriceHunt.Infrastructure.Suppliers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddBrowserClientCors(builder.Configuration);
builder.Services.AddSearchOptions(builder.Configuration);
builder.Services.AddPriceSearch();
builder.Services.AddSimulatedSuppliers();
builder.Services.AddSqlitePersistence(
    builder.Configuration.GetConnectionString("PriceHunt")
        ?? throw new InvalidOperationException("ConnectionStrings:PriceHunt is not configured."));
builder.Services.AddSingleton<ISearchQueryParser, SearchQueryParser>();
builder.Services.AddSingleton<IHistoryQueryParser, HistoryQueryParser>();
builder.Services.AddHealthChecks().AddDbContextCheck<PriceHuntDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors();

await app.Services.MigrateDatabaseAsync(CancellationToken.None);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.MapSuppliers();
app.MapSearch();
app.MapHistory();

await app.RunAsync();

/// <summary>
/// Exposes the entry point to the integration tests' WebApplicationFactory.
/// </summary>
public partial class Program;
