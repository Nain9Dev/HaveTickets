namespace HaveTickets.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid UserId { get; set; } // Sesión anónima del reclutador (localStorage)
    
    public Guid ShowtimeId { get; set; }
    public Showtime Showtime { get; set; } = null!;

    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public string SeatNumber { get; set; } = string.Empty;
    public string QRToken { get; set; } = Guid.NewGuid().ToString("N");
}
