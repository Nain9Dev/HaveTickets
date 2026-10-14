using HaveTickets.Domain.Catalog;
using HaveTickets.Domain.Scheduling;
using HaveTickets.Domain.Ticketing;
using HaveTickets.Domain.Venues;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Infrastructure.Persistence;

public sealed class HaveTicketsDbContext(DbContextOptions<HaveTicketsDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<Cinema> Cinemas => Set<Cinema>();

    public DbSet<Auditorium> Auditoriums => Set<Auditorium>();

    public DbSet<Showtime> Showtimes => Set<Showtime>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HaveTicketsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        if (Database.IsSqlite())
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var dateTimeOffsetProperties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?));

                foreach (var property in dateTimeOffsetProperties)
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}
