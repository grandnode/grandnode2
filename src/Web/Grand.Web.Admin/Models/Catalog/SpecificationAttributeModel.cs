using Grand.Web.Common.Localization;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;
using Grand.Web.Common.Models;
using Grand.Web.Common.Link;
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

    }

    public partial class SpecificationAttributeLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Catalog.Attributes.SpecificationAttributes.Fields.Name")]
        public string Name { get; set; }
    }
}