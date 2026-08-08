using HaveTickets.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Infrastructure.Persistence;

public class HaveTicketsDbContext : DbContext
{
    public HaveTicketsDbContext(DbContextOptions<HaveTicketsDbContext> options) : base(options)
    {
    }

    public DbSet<Movie> Movies { get; set; } = null!;
    public DbSet<Cinema> Cinemas { get; set; } = null!;
    public DbSet<Showtime> Showtimes { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Movie)
                .WithMany(m => m.Showtimes)
                .HasForeignKey(e => e.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Cinema)
                .WithMany(c => c.Showtimes)
                .HasForeignKey(e => e.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Showtime)
                .WithMany(s => s.Tickets)
                .HasForeignKey(e => e.ShowtimeId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Asignar asiento automáticamente para la demo
            entity.Property(e => e.SeatNumber).HasDefaultValue("General");
        });
    }
}
