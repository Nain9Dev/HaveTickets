namespace HaveTickets.Application.DTOs;

public class MovieDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public DateTime ReleaseDate { get; set; }
    public string Genre { get; set; } = string.Empty;

    public List<ShowtimeDto> Showtimes { get; set; } = new();
}

public class CinemaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class ShowtimeDto
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public decimal Price { get; set; }
    
    public CinemaDto Cinema { get; set; } = null!;
}

public class TicketDto
{
    public Guid Id { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string QRToken { get; set; } = string.Empty;
    
    public string MovieTitle { get; set; } = string.Empty;
    public string CinemaName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
}

public class BuyTicketRequest
{
    public Guid ShowtimeId { get; set; }
    public Guid UserId { get; set; }
}
