namespace Grand.Domain.Audit;

/// <summary>
///     Represents one record of the staff audit trail. Entries are never updated; only the retention task deletes them.
/// </summary>
public class AuditEntry : BaseEntity
{
    public AuditKind Kind { get; set; }

    public AuditSource Source { get; set; }

    /// <summary>
    ///     Gets or sets the staff member who made the change
    /// </summary>
    public string ActorCustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the actor's e-mail at the time of the change
    /// </summary>
    public string ActorEmail { get; set; }

    /// <summary>
    ///     Gets or sets the customer the actor was impersonating, if any
    /// </summary>
    public string ImpersonatedCustomerId { get; set; }

    /// <summary>
    ///     Gets or sets the actor's staff store
    /// </summary>
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets the actor's vendor
    /// </summary>
    public string VendorId { get; set; }

    public string IpAddress { get; set; }

    public string UserAgent { get; set; }

    /// <summary>
    ///     Gets or sets the request's intent as "Area / Controller / Action"
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    ///     Gets or sets the identifier shared by every entry of one request or import
    /// </summary>
    public string OperationId { get; set; }

    public string EntityType { get; set; }

    public string EntityId { get; set; }

    /// <summary>
    ///     Gets or sets a display label of the entity at the time of the change
    /// </summary>
    public string EntityName { get; set; }

    /// <summary>
    ///     Gets or sets the store the changed record belongs to (a setting: empty for all stores); null when the
    ///     record has no store
    /// </summary>
    public string EntityStoreId { get; set; }

    /// <summary>
    ///     Gets or sets the type of the record the changed record belongs to (Order for an order note, see
    ///     <see cref="AuditOwnerAttribute" />); null when it belongs to none
    /// </summary>
    public string OwnerType { get; set; }

    /// <summary>
    ///     Gets or sets the id of the record the changed record belongs to
    /// </summary>
    public string OwnerId { get; set; }

    public IList<AuditChange> Changes { get; set; } = new List<AuditChange>();

    /// <summary>
    ///     Gets or sets free text for events and bulk operations
    /// </summary>
    public string Details { get; set; }
}
