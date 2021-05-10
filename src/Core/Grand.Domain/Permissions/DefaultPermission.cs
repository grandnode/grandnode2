using System.Collections.Generic;

namespace Grand.Domain.Permissions
{
    /// <summary>
    /// Represents a default permission
    /// </summary>
    public class DefaultPermission
    {
        public DefaultPermission() 
        {
            Permissions = new List<Permission>();
        }

        /// <summary>
        /// Gets or sets the customer group system name
        /// </summary>
        public string CustomerGroupSystemName { get; set; }

        /// <summary>
        /// Gets or sets the permissions
        /// </summary>
        public IEnumerable<Permission> Permissions { get; set; }
    }
}
