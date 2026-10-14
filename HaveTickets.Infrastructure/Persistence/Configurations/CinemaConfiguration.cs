using HaveTickets.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HaveTickets.Infrastructure.Persistence.Configurations;

public sealed class CinemaConfiguration : IEntityTypeConfiguration<Cinema>
{
    public void Configure(EntityTypeBuilder<Cinema> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(cinema => cinema.Id);

        builder.Property(cinema => cinema.Name).IsRequired().HasMaxLength(150);
        builder.Property(cinema => cinema.City).HasMaxLength(100);
        builder.Property(cinema => cinema.Address).HasMaxLength(250);
        builder.Property(cinema => cinema.ImageUrl).HasMaxLength(500);

        builder.Metadata
            .FindNavigation(nameof(Cinema.Auditoriums))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
