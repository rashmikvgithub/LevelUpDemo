namespace LevelUp.Domain.Common;

/// <summary>
/// Base type for all persisted entities. Audit fields are populated automatically by the persistence layer.
/// </summary>
public abstract class AuditableEntity
{
    public int Id { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
