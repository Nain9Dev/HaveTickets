using HaveTickets.Application.DTOs;
using HaveTickets.Domain.Entities;
using HaveTickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Api.Endpoints;

public static class MovieEndpoints
{
    public static void MapMovieEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/movies").WithTags("Movies");

        group.MapGet("/", async (HaveTicketsDbContext db) =>
        {
            var movies = await db.Movies
                .Include(m => m.Showtimes)
                    .ThenInclude(s => s.Cinema)
                .Select(m => new MovieDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    PosterUrl = m.PosterUrl,
                    DurationMinutes = m.DurationMinutes,
                    ReleaseDate = m.ReleaseDate,
                    Genre = m.Genre,
                    Showtimes = m.Showtimes.Select(s => new ShowtimeDto
                    {
                        Id = s.Id,
                        StartTime = s.StartTime,
                        Price = s.Price,
                        Cinema = new CinemaDto
                        {
                            Id = s.Cinema.Id,
                            Name = s.Cinema.Name,
                            Location = s.Cinema.Location
                        }
                    }).ToList()
                })
                .ToListAsync();

            return Results.Ok(movies);
        })
        .RequireRateLimiting("fixed");
    }
}
