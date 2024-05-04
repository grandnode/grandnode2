using Grand.Domain.Common;

namespace Grand.Domain;

/// <summary>
///     Base class for entities
/// </summary>
public abstract class BaseEntity : ParentEntity, IAuditableEntity
{
    public IList<UserField> UserFields { get; set; } = new List<UserField>();

    public DateTime CreatedOnUtc { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public string UpdatedBy { get; set; }
}