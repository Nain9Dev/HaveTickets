using HaveTickets.Application.UseCases.Catalog;
using HaveTickets.Application.UseCases.Ticketing;
using Microsoft.Extensions.DependencyInjection;

namespace HaveTickets.Application;

/// <summary>
/// Registers the use cases.
/// <para>
/// Handlers are plain classes resolved directly from the container. There is no
/// mediator: a dispatcher would add indirection, a runtime registration scan and
/// a licence to worry about, in exchange for hiding a constructor call.
/// </para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingletonTimeProvider();

        services.AddScoped<ListMoviesHandler>();
        services.AddScoped<GetMovieHandler>();
        services.AddScoped<GetSeatMapHandler>();
        services.AddScoped<ListGenresHandler>();

        services.AddScoped<PurchaseTicketsHandler>();
        services.AddScoped<ListTicketsHandler>();
        services.AddScoped<CancelTicketHandler>();
        services.AddScoped<CheckInTicketHandler>();

        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(TimeProvider)))
        {
            return;
        }

        services.AddSingleton(TimeProvider.System);
    }
}
