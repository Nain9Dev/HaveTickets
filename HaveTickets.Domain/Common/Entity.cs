namespace HaveTickets.Domain.Common;

/// <summary>
/// Base class for entities: identity-based equality, identifier assigned at creation.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id) => Id = id;

    /// <summary>Required by EF Core materialisation.</summary>
    protected Entity()
    {
    }

    public Guid Id { get; private set; }

    public bool Equals(Entity? other) =>
        other is not null && other.GetType() == GetType() && other.Id == Id;

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
