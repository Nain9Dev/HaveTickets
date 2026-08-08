using HaveTickets.Application.DTOs;
using HaveTickets.Domain.Entities;
using HaveTickets.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Api.Endpoints;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tickets").WithTags("Tickets");

        group.MapPost("/buy", async (BuyTicketRequest request, HaveTicketsDbContext db) =>
        {
            var showtime = await db.Showtimes.FindAsync(request.ShowtimeId);
            if (showtime == null)
            {
                return Results.NotFound("Showtime not found.");
            }

            var ticket = new Ticket
            {
                UserId = request.UserId,
                ShowtimeId = request.ShowtimeId,
                // SeatNumber and QRToken are generated automatically by defaults/entity
            };

            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();

            return Results.Ok(new { TicketId = ticket.Id, QRToken = ticket.QRToken });
        })
        .RequireRateLimiting("fixed");

        group.MapGet("/user/{userId:guid}", async (Guid userId, HaveTicketsDbContext db) =>
        {
            var tickets = await db.Tickets
                .Include(t => t.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(t => t.Showtime)
                    .ThenInclude(s => s.Cinema)
                .Where(t => t.UserId == userId)
                .Select(t => new TicketDto
                {
                    Id = t.Id,
                    PurchaseDate = t.PurchaseDate,
                    SeatNumber = t.SeatNumber,
                    QRToken = t.QRToken,
                    MovieTitle = t.Showtime.Movie.Title,
                    CinemaName = t.Showtime.Cinema.Name,
                    StartTime = t.Showtime.StartTime,
                    PosterUrl = t.Showtime.Movie.PosterUrl
                })
                .OrderByDescending(t => t.PurchaseDate)
                .ToListAsync();

            return Results.Ok(tickets);
        })
        .RequireRateLimiting("fixed");
    }
}
