using HaveTickets.Application.Abstractions;
using HaveTickets.Domain.Common;
using HaveTickets.Domain.Ticketing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HaveTickets.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(HaveTicketsDbContext database) : IUnitOfWork
{
    private const int SqliteConstraintViolation = 19;

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (IsConstraintViolation(exception))
        {
            // The unique index on (ShowtimeId, Seat) fired: another buyer got there
            // first. EF has already rolled the transaction back, so nothing partial
            // was written. Translate the storage failure into the domain vocabulary.
            database.ChangeTracker.Clear();

            string? contestedSeat = exception.Entries
                .Select(entry => entry.Entity)
                .OfType<Ticket>()
                .Select(ticket => ticket.Seat.Label)
                .FirstOrDefault();

            return contestedSeat is null
                ? Error.Conflict("seat.already_taken", "One of the selected seats has just been sold.")
                : DomainErrors.Seat.AlreadyTaken(contestedSeat);
        }
    }

    private static bool IsConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintViolation };
}
