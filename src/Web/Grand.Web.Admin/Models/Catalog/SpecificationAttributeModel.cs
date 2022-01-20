using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Catalog
{
    public partial class SpecificationAttributeModel : BaseEntityModel, ILocalizedModel<SpecificationAttributeLocalizedModel>, IStoreLinkModel
    {
        public SpecificationAttributeModel()
        {
            Locales = new List<SpecificationAttributeLocalizedModel>();
        }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Fields.SeName")]
        public string SeName { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        //Store acl
        [GrandResourceDisplayName("Admin.Catalog.Categories.Fields.LimitedToStores")]
        [UIHint("Stores")]
        public string[] Stores { get; set; }

        public IList<SpecificationAttributeLocalizedModel> Locales { get; set; }

        public partial class UsedByProductModel : BaseEntityModel
        {
            [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.UsedByProducts.Product")]
            public string ProductName { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.UsedByProducts.OptionName")]
            public string OptionName { get; set; }
            [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.UsedByProducts.Published")]
            public bool Published { get; set; }
        }
    }

    public partial class SpecificationAttributeLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Fields.Name")]
        public string Name { get; set; }
    }


}