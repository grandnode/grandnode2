namespace Grand.Infrastructure.Models;

/// <summary>
///     Represents base grandnode entity model
/// </summary>
public class BaseEntityModel : BaseModel
{
    /// <summary>
    ///     Gets or sets model identifier
    /// </summary>
    public virtual string Id { get; set; }
}