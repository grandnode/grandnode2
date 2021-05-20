using Grand.Domain.Common;
using System.Collections.Generic;

namespace Grand.Domain
{
    /// <summary>
    /// Base class for entities
    /// </summary>
    public abstract partial class BaseEntity : ParentEntity
    {
        protected BaseEntity()
        {
            UserFields = new List<UserField>();
        }

        public IList<UserField> UserFields { get; set; }

    }
}
