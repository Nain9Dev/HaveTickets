using HaveTickets.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace HaveTickets.Infrastructure.Persistence;

public static class DataSeeder
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HaveTicketsDbContext>();

        context.Database.EnsureCreated();

        if (context.Movies.Any())
        {
            return;   // DB has been seeded
        }

        var movies = new[]
        {
            new Movie
            {
                Title = "The Matrix Resurrections",
                Description = "Return to a world of two realities: one, everyday life; the other, what lies behind it.",
                PosterUrl = "https://picsum.photos/seed/matrix/400/600",
                DurationMinutes = 148,
                ReleaseDate = new DateTime(2021, 12, 22).ToUniversalTime(),
                Genre = "Sci-Fi"
            },
            new Movie
            {
                Title = "Dune: Part Two",
                Description = "Paul Atreides unites with Chani and the Fremen while on a warpath of revenge.",
                PosterUrl = "https://picsum.photos/seed/dune/400/600",
                DurationMinutes = 166,
                ReleaseDate = new DateTime(2024, 3, 1).ToUniversalTime(),
                Genre = "Sci-Fi"
            },
            new Movie
            {
                Title = "Oppenheimer",
                Description = "The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb.",
                PosterUrl = "https://picsum.photos/seed/oppen/400/600",
                DurationMinutes = 180,
                ReleaseDate = new DateTime(2023, 7, 21).ToUniversalTime(),
                Genre = "Drama"
            },
            new Movie
            {
                Title = "Spider-Man: Across the Spider-Verse",
                Description = "Miles Morales catapults across the Multiverse, where he encounters a team of Spider-People.",
                PosterUrl = "https://picsum.photos/seed/spider/400/600",
                DurationMinutes = 140,
                ReleaseDate = new DateTime(2023, 6, 2).ToUniversalTime(),
                Genre = "Animation"
            }
        };

        context.Movies.AddRange(movies);
        context.SaveChanges();

        var cinemas = new[]
        {
            new Cinema
            {
                Name = "Cineplex Downtown",
                Location = "123 Main St, City Center",
                ImageUrl = "https://picsum.photos/seed/cineplex/800/400"
            },
            new Cinema
            {
                Name = "Starlight Drive-In",
                Location = "456 Highway 1, Outskirts",
                ImageUrl = "https://picsum.photos/seed/starlight/800/400"
            }
        };

        context.Cinemas.AddRange(cinemas);
        context.SaveChanges();

        var showtimes = new[]
        {
            new Showtime
            {
                MovieId = movies[0].Id,
                CinemaId = cinemas[0].Id,
                StartTime = DateTime.UtcNow.AddDays(1).AddHours(19),
                Price = 12.50m
            },
            new Showtime
            {
                MovieId = movies[1].Id,
                CinemaId = cinemas[0].Id,
                StartTime = DateTime.UtcNow.AddDays(1).AddHours(21),
                Price = 14.00m
            },
            new Showtime
            {
                MovieId = movies[2].Id,
                CinemaId = cinemas[1].Id,
                StartTime = DateTime.UtcNow.AddDays(2).AddHours(20),
                Price = 10.00m
            },
            new Showtime
            {
                MovieId = movies[3].Id,
                CinemaId = cinemas[1].Id,
                StartTime = DateTime.UtcNow.AddDays(2).AddHours(18),
                Price = 11.50m
            }
        };

        context.Showtimes.AddRange(showtimes);
        context.SaveChanges();
    }
}
