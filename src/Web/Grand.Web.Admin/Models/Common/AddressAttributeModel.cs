﻿using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Common
{
    public partial class AddressAttributeModel : BaseEntityModel, ILocalizedModel<AddressAttributeLocalizedModel>
    {
        public AddressAttributeModel()
        {
            Locales = new List<AddressAttributeLocalizedModel>();
        }

        [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.IsRequired")]
        public bool IsRequired { get; set; }

        [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.AttributeControlType")]
        public int AttributeControlTypeId { get; set; }
        [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.AttributeControlType")]

        public string AttributeControlTypeName { get; set; }

        [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }


        public IList<AddressAttributeLocalizedModel> Locales { get; set; }

    }

    public partial class AddressAttributeLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Address.AddressAttributes.Fields.Name")]

        public string Name { get; set; }

    }
}