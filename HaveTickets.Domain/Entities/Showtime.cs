namespace HaveTickets.Domain.Entities;

public class Showtime : BaseEntity
{
    public Guid MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public Guid CinemaId { get; set; }
    public Cinema Cinema { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
