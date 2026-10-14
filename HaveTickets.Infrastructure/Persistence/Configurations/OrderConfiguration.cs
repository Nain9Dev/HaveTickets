using HaveTickets.Domain.Ticketing;
using HaveTickets.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HaveTickets.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(order => order.Id);

        builder.Property(order => order.HolderId).IsRequired();
        builder.Property(order => order.PlacedAt).IsRequired();

        builder.Property(order => order.Reference)
            .HasConversion<ConfirmationCodeConverter>()
            .HasMaxLength(8)
            .IsRequired();

        builder.OwnsOne(order => order.Total, total =>
        {
            total.Property(money => money.AmountInMinorUnits)
                .HasColumnName("TotalAmountMinorUnits")
                .IsRequired();

            total.Property(money => money.Currency)
                .HasColumnName("TotalCurrency")
                .HasMaxLength(3)
                .IsRequired();

            total.Ignore(money => money.Amount);
        });
        builder.Navigation(order => order.Total).IsRequired();

        builder.HasOne(order => order.Showtime)
            .WithMany()
            .HasForeignKey(order => order.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Order.Tickets))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(order => order.Reference).IsUnique();
        builder.HasIndex(order => order.HolderId);
    }
}
