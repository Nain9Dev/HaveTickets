using HaveTickets.Application.Abstractions;
using HaveTickets.Infrastructure.Persistence;
using HaveTickets.Infrastructure.Persistence.ReadModels;
using HaveTickets.Infrastructure.Persistence.Repositories;
using HaveTickets.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HaveTickets.Infrastructure;

/// <summary>
/// Binds the ports declared in the application layer to their SQLite and QRCoder
/// implementations. This is the only place in the solution that knows which
/// database is in use.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<HaveTicketsDbContext>(options => options
            .UseSqlite(connectionString, sqlite => sqlite
                .MigrationsAssembly(typeof(HaveTicketsDbContext).Assembly.FullName)));

        services.AddScoped<IShowtimeRepository, ShowtimeRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICatalogReadModel, CatalogReadModel>();
        services.AddScoped<ITicketReadModel, TicketReadModel>();

        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

        return services;
    }
}
