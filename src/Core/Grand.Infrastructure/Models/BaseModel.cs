using Grand.Domain.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Grand.Infrastructure.Models
{
    /// <summary>
    /// Represents base model
    /// </summary>
    public partial class BaseModel
    {
        #region Ctor

        public BaseModel()
        {
            UserFields = new List<UserField>();
            PostInitialize();
        }

        #endregion

        #region Methods

        public virtual void BindModel(ModelBindingContext bindingContext)
        {
        }

        protected virtual void PostInitialize()
        {            
        }

        #endregion

        #region Properties        

        public IList<UserField> UserFields { get; set; }

        #endregion

    }
}
