using HaveTickets.Api.Endpoints;
using HaveTickets.Api.Extensions;
using HaveTickets.Application;
using HaveTickets.Infrastructure;
using HaveTickets.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("HaveTickets")
    ?? "Data Source=havetickets.db";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddForwardedHeaders();
builder.Services.AddConfiguredCors(builder.Configuration);
builder.Services.AddConfiguredRateLimiting(builder.Configuration);
builder.Services.AddConfiguredHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    HaveTicketsDbContext database = scope.ServiceProvider.GetRequiredService<HaveTicketsDbContext>();
    await database.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(database, scope.ServiceProvider.GetRequiredService<TimeProvider>());
}

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();

app.MapOpenApi();
app.MapScalarApiReference("/docs");

app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapCatalogEndpoints();
app.MapTicketEndpoints();

await app.RunAsync();

/// <summary>Entry point marker so integration tests can host the API in memory.</summary>
public partial class Program;
