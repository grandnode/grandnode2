﻿using Grand.Domain.Catalog;
using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Customer
{
    public class CustomerAttributeModel : BaseEntityModel
    {
        public CustomerAttributeModel()
        {
            Values = new List<CustomerAttributeValueModel>();
        }

        public string Name { get; set; }

        public bool IsRequired { get; set; }

        public bool IsReadOnly { get; set; }
        /// <summary>
        /// Default value for textboxes
        /// </summary>
        public string DefaultValue { get; set; }

        public AttributeControlType AttributeControlType { get; set; }

        public IList<CustomerAttributeValueModel> Values { get; set; }

    }

    public class CustomerAttributeValueModel : BaseEntityModel
    {
        public string Name { get; set; }

        public bool IsPreSelected { get; set; }
    }
}