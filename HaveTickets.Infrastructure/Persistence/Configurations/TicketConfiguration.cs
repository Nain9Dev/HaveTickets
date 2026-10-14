using HaveTickets.Domain.Ticketing;
using HaveTickets.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HaveTickets.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.HolderId).IsRequired();
        builder.Property(ticket => ticket.IssuedAt).IsRequired();

        builder.Property(ticket => ticket.Seat)
            .HasConversion<SeatNumberConverter>()
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(ticket => ticket.Code)
            .HasConversion<ConfirmationCodeConverter>()
            .HasMaxLength(8)
            .IsRequired();

        // Stored as text: a migration that reorders the enum must not silently
        // turn every cancelled ticket into a valid one.
        builder.Property(ticket => ticket.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.OwnsOne(ticket => ticket.PricePaid, price =>
        {
            price.Property(money => money.AmountInMinorUnits)
                .HasColumnName("PricePaidAmountMinorUnits")
                .IsRequired();

            price.Property(money => money.Currency)
                .HasColumnName("PricePaidCurrency")
                .HasMaxLength(3)
                .IsRequired();

            price.Ignore(money => money.Amount);
        });
        builder.Navigation(ticket => ticket.PricePaid).IsRequired();

        builder.Ignore(ticket => ticket.OccupiesSeat);

        builder.HasOne(ticket => ticket.Order)
            .WithMany(order => order.Tickets)
            .HasForeignKey(ticket => ticket.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ticket => ticket.Showtime)
            .WithMany(showtime => showtime.Tickets)
            .HasForeignKey(ticket => ticket.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ticket => ticket.Code).IsUnique();
        builder.HasIndex(ticket => ticket.HolderId);
        builder.HasIndex(ticket => ticket.ShowtimeId);

        // The invariant that makes overbooking impossible.
        //
        // Application code checks availability first so that buyers get a clear
        // message, but that check races: two requests can both read a seat as
        // free. This partial unique index is the arbiter. Whichever INSERT reaches
        // the database second is rejected by SQLite, the transaction rolls back,
        // and the unit of work turns the constraint violation into the domain
        // error seat.already_taken. Cancelled tickets are excluded from the index,
        // so cancelling a ticket genuinely puts the seat back on sale.
        builder.HasIndex(ticket => new { ticket.ShowtimeId, ticket.Seat })
            .IsUnique()
            .HasFilter("\"Status\" <> 'Cancelled'")
            .HasDatabaseName("UX_Tickets_ShowtimeId_Seat_Active");
    }
}
