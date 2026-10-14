using HaveTickets.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HaveTickets.Infrastructure.Persistence.Configurations;

public sealed class AuditoriumConfiguration : IEntityTypeConfiguration<Auditorium>
{
    public void Configure(EntityTypeBuilder<Auditorium> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(auditorium => auditorium.Id);

        builder.Property(auditorium => auditorium.Name).IsRequired().HasMaxLength(100);
        builder.Property(auditorium => auditorium.RowCount).IsRequired();
        builder.Property(auditorium => auditorium.SeatsPerRow).IsRequired();

        // Capacity is derived from the layout and is never stored.
        builder.Ignore(auditorium => auditorium.Capacity);

        builder.HasOne(auditorium => auditorium.Cinema)
            .WithMany(cinema => cinema.Auditoriums)
            .HasForeignKey(auditorium => auditorium.CinemaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(auditorium => new { auditorium.CinemaId, auditorium.Name }).IsUnique();
    }
}
