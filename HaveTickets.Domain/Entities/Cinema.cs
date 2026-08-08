namespace HaveTickets.Domain.Entities;

public class Cinema : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
