using Grand.Domain.Catalog;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders
{
    public partial class CheckoutAttributeModel : BaseEntityModel, ILocalizedModel<CheckoutAttributeLocalizedModel>, IGroupLinkModel, IStoreLinkModel
    {
        public CheckoutAttributeModel()
        {
            Locales = new List<CheckoutAttributeLocalizedModel>();
            AvailableTaxCategories = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.TextPrompt")]

        public string TextPrompt { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.IsRequired")]
        public bool IsRequired { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.ShippableProductRequired")]
        public bool ShippableProductRequired { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.IsTaxExempt")]
        public bool IsTaxExempt { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.TaxCategory")]
        public string TaxCategoryId { get; set; }
        public IList<SelectListItem> AvailableTaxCategories { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.AttributeControlType")]
        public int AttributeControlTypeId { get; set; }
        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.AttributeControlType")]

        public string AttributeControlTypeName { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }


        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.MinLength")]
        [UIHint("Int32Nullable")]
        public int? ValidationMinLength { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.MaxLength")]
        [UIHint("Int32Nullable")]
        public int? ValidationMaxLength { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.FileAllowedExtensions")]
        public string ValidationFileAllowedExtensions { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.FileMaximumSize")]
        [UIHint("Int32Nullable")]
        public int? ValidationFileMaximumSize { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.DefaultValue")]
        public string DefaultValue { get; set; }

        public IList<CheckoutAttributeLocalizedModel> Locales { get; set; }

        //condition
        public bool ConditionAllowed { get; set; }
        public ConditionModel ConditionModel { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }
        //ACL
        [UIHint("CustomerGroups")]
        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.LimitedToGroups")]
        public string[] CustomerGroups { get; set; }
    }

    public partial class ConditionModel : BaseEntityModel
    {
        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Condition.EnableCondition")]
        public bool EnableCondition { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Condition.Attributes")]
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
    public partial class CheckoutAttributeLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Orders.CheckoutAttributes.Fields.TextPrompt")]

        public string TextPrompt { get; set; }

    }
}