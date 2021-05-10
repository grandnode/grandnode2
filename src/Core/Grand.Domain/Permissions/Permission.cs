using System.Collections.Generic;

namespace Grand.Domain.Permissions
{
    /// <summary>
    /// Represents a permission
    /// </summary>
    public partial class Permission : BaseEntity
    {
        private ICollection<string> _customerGroups;
        private ICollection<string> _actions;

        /// <summary>
        /// Gets or sets the permission name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the permission system name
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets the area name
        /// </summary>
        public string Area { get; set; }

        /// <summary>
        /// Gets or sets the permission category
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets customer groups
        /// </summary>
        public virtual ICollection<string> CustomerGroups
        {
            get { return _customerGroups ??= new List<string>(); }
            protected set { _customerGroups = value; }
        }

        /// <summary>
        /// Gets or sets actions
        /// </summary>
        public virtual ICollection<string> Actions {
            get { return _actions ??= new List<string>(); }
            set { _actions = value; }
        }
    }
}
