namespace SmartTask.Domain.Common;

/// <summary>
/// Tüm entity'lerin türediği generic base class.
/// Id tipi generic tutularak hem Guid hem int desteklenir.
/// </summary>
public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; } = false;

    public void SetUpdated() => UpdatedAt = DateTime.UtcNow;
    public void SoftDelete()
    {
        IsDeleted = true;
        SetUpdated();
    }
}

/// <summary>
/// Guid Id kullanan entity'ler için kısayol.
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity() => Id = Guid.NewGuid();
}
