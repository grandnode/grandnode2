namespace Grand.Domain.Permissions;

/// <summary>
///     Represents an entity which entity group linking
/// </summary>
public interface IGroupLinkEntity
{
    /// <summary>
    ///     Gets or sets a value indicating whether the entity is subject to group
    /// </summary>
    bool LimitedToGroups { get; set; }

    IList<string> CustomerGroups { get; set; }
}