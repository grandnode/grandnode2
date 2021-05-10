using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class ContactAttributeModel : BaseEntityModel, ILocalizedModel<ContactAttributeLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public ContactAttributeModel()
        {
            Locales = new List<ContactAttributeLocalizedModel>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.TextPrompt")]

        public string TextPrompt { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.IsRequired")]
        public bool IsRequired { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.AttributeControlType")]
        public int AttributeControlTypeId { get; set; }
        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.AttributeControlType")]

        public string AttributeControlTypeName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }


        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.MinLength")]
        [UIHint("Int32Nullable")]
        public int? ValidationMinLength { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.MaxLength")]
        [UIHint("Int32Nullable")]
        public int? ValidationMaxLength { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.FileAllowedExtensions")]
        public string ValidationFileAllowedExtensions { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.FileMaximumSize")]
        [UIHint("Int32Nullable")]
        public int? ValidationFileMaximumSize { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.DefaultValue")]
        public string DefaultValue { get; set; }

        public IList<ContactAttributeLocalizedModel> Locales { get; set; }

        //condition
        public bool ConditionAllowed { get; set; }
        public ConditionModel ConditionModel { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }
    }

    public partial class ConditionModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Condition.EnableCondition")]
        public bool EnableCondition { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Condition.Attributes")]
        public string SelectedAttributeId { get; set; }

        public IList<AttributeConditionModel> ConditionAttributes { get; set; }
    }
    public partial class AttributeConditionModel : BaseEntityModel
    {
        public string Name { get; set; }

        public AttributeControlType AttributeControlType { get; set; }

        public IList<SelectListItem> Values { get; set; }

        public string SelectedValueId { get; set; }
    }
    public partial class ContactAttributeLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.ContactAttributes.Fields.TextPrompt")]

        public string TextPrompt { get; set; }

    }
}