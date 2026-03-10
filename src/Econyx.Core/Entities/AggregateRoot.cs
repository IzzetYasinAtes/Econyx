namespace Econyx.Core.Entities;

public abstract class AggregateRoot<TId> : BaseEntity<TId> where TId : notnull
{
    public int Version { get; protected set; }

    protected void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
