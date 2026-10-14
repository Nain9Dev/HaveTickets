using HaveTickets.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HaveTickets.Infrastructure.Persistence.Configurations;

public sealed class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(showtime => showtime.Id);

        builder.Property(showtime => showtime.StartsAt).IsRequired();

        // Money is two columns: integer minor units plus an ISO 4217 code.
        builder.OwnsOne(showtime => showtime.Price, price =>
        {
            price.Property(money => money.AmountInMinorUnits)
                .HasColumnName("PriceAmountMinorUnits")
                .IsRequired();

            price.Property(money => money.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();

            price.Ignore(money => money.Amount);
        });
        builder.Navigation(showtime => showtime.Price).IsRequired();

        // Sales and check-in windows are pure functions of StartsAt.
        builder.Ignore(showtime => showtime.SalesCloseAt);
        builder.Ignore(showtime => showtime.CheckInOpensAt);
        builder.Ignore(showtime => showtime.CheckInClosesAt);

        builder.HasOne(showtime => showtime.Movie)
            .WithMany(movie => movie.Showtimes)
            .HasForeignKey(showtime => showtime.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(showtime => showtime.Auditorium)
            .WithMany()
            .HasForeignKey(showtime => showtime.AuditoriumId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Metadata
            .FindNavigation(nameof(Showtime.Tickets))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(showtime => showtime.StartsAt);
        builder.HasIndex(showtime => new { showtime.MovieId, showtime.StartsAt });

        // One room cannot host two films at the same instant.
        builder.HasIndex(showtime => new { showtime.AuditoriumId, showtime.StartsAt }).IsUnique();
    }
}
