﻿using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Customers
{
    public class CustomerAttributeValueModel : BaseEntityModel, ILocalizedModel<CustomerAttributeValueLocalizedModel>
    {
        public string CustomerAttributeId { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Values.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Values.Fields.IsPreSelected")]
        public bool IsPreSelected { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Values.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<CustomerAttributeValueLocalizedModel> Locales { get; set; } = new List<CustomerAttributeValueLocalizedModel>();
    }

    public class CustomerAttributeValueLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Customers.CustomerAttributes.Values.Fields.Name")]

        public string Name { get; set; }
    }
}