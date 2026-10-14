using HaveTickets.Domain.Common;
using HaveTickets.Domain.ValueObjects;

namespace HaveTickets.Domain.Venues;

/// <summary>
/// A screening room with a rectangular seat map. The layout is the single source
/// of truth for which seats exist, so an order can never reference a seat the
/// room does not physically have.
/// </summary>
public sealed class Auditorium : Entity
{
    private Auditorium(Guid id, Guid cinemaId, string name, int rowCount, int seatsPerRow)
        : base(id)
    {
        CinemaId = cinemaId;
        Name = name;
        RowCount = rowCount;
        SeatsPerRow = seatsPerRow;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private Auditorium() => Name = string.Empty;

    public Guid CinemaId { get; private set; }

    public Cinema? Cinema { get; private set; }

    public string Name { get; private set; }

    public int RowCount { get; private set; }

    public int SeatsPerRow { get; private set; }

    public int Capacity => RowCount * SeatsPerRow;

    public static Result<Auditorium> Create(Guid id, Guid cinemaId, string name, int rowCount, int seatsPerRow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Auditorium.NameRequired;
        }

        if (rowCount is < 1 or > SeatNumber.MaxRow || seatsPerRow is < 1 or > SeatNumber.MaxNumber)
        {
            return DomainErrors.Auditorium.InvalidLayout;
        }

        return new Auditorium(id, cinemaId, name.Trim(), rowCount, seatsPerRow);
    }

    /// <summary>True when the seat exists in this room.</summary>
    public bool Contains(SeatNumber seat) => seat.Row <= RowCount && seat.Number <= SeatsPerRow;

    /// <summary>Every seat in the room, ordered row by row.</summary>
    public IEnumerable<SeatNumber> AllSeats()
    {
        for (int row = 1; row <= RowCount; row++)
        {
            for (int number = 1; number <= SeatsPerRow; number++)
            {
                yield return SeatNumber.Create(row, number).Value;
            }
        }
    }
}
