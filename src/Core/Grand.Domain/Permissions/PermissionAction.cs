namespace Grand.Domain.Permissions
{
    /// <summary>
    /// Represents permission for action denied
    /// </summary>
    public partial class PermissionAction : BaseEntity
    {

        /// <summary>
        /// Gets or sets the permission system name
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets the customer group ident
        /// </summary>
        public string CustomerGroupId { get; set; }

        /// <summary>
        /// Gets or sets the action name for denied access
        /// </summary>
        public string Action { get; set; }

    }
}
