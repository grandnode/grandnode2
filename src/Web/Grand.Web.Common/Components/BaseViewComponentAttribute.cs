using System;

namespace Grand.Web.Common.Components
{
    public class BaseViewComponentAttribute : Attribute
    {
        public bool AdminAccess { get; set; }
    }
}
