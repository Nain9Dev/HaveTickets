using HaveTickets.Domain.Common;

namespace HaveTickets.Domain.Venues;

/// <summary>A physical venue containing one or more auditoriums.</summary>
public sealed class Cinema : Entity
{
    private readonly List<Auditorium> _auditoriums = [];

    private Cinema(Guid id, string name, string city, string address, string imageUrl)
        : base(id)
    {
        Name = name;
        City = city;
        Address = address;
        ImageUrl = imageUrl;
    }

    /// <summary>Required by EF Core materialisation.</summary>
    private Cinema()
    {
        Name = string.Empty;
        City = string.Empty;
        Address = string.Empty;
        ImageUrl = string.Empty;
    }

    public string Name { get; private set; }

    public string City { get; private set; }

    public string Address { get; private set; }

    public string ImageUrl { get; private set; }

    public IReadOnlyCollection<Auditorium> Auditoriums => _auditoriums.AsReadOnly();

    public static Result<Cinema> Create(Guid id, string name, string city, string address, string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return DomainErrors.Cinema.NameRequired;
        }

        return new Cinema(
            id,
            name.Trim(),
            city?.Trim() ?? string.Empty,
            address?.Trim() ?? string.Empty,
            imageUrl?.Trim() ?? string.Empty);
    }

    public Result<Auditorium> AddAuditorium(Guid id, string name, int rowCount, int seatsPerRow)
    {
        Result<Auditorium> auditorium = Auditorium.Create(id, Id, name, rowCount, seatsPerRow);
        if (auditorium.IsFailure)
        {
            return auditorium;
        }

        _auditoriums.Add(auditorium.Value);
        return auditorium;
    }
}
