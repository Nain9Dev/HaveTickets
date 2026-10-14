using HaveTickets.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HaveTickets.Infrastructure.Persistence.Configurations;

public sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(movie => movie.Id);

        builder.Property(movie => movie.Title).IsRequired().HasMaxLength(200);
        builder.Property(movie => movie.Synopsis).HasMaxLength(2000);
        builder.Property(movie => movie.PosterUrl).HasMaxLength(500);
        builder.Property(movie => movie.Genre).HasMaxLength(60);
        builder.Property(movie => movie.AgeRating).HasMaxLength(10);

        builder.HasIndex(movie => movie.Genre);
        builder.HasIndex(movie => movie.Title);

        // The collection is exposed read-only; EF writes through the backing field.
        builder.Metadata
            .FindNavigation(nameof(Movie.Showtimes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
