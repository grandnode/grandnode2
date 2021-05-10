using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class InteractiveFormAttributeValueModel : BaseEntityModel, ILocalizedModel<InteractiveFormAttributeValueLocalizedModel>
    {
        public InteractiveFormAttributeValueModel()
        {
            Locales = new List<InteractiveFormAttributeValueLocalizedModel>();
        }
        public string FormId { get; set; }
        public string AttributeId { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Values.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Values.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Values.Fields.IsPreSelected")]
        public bool IsPreSelected { get; set; }

        public IList<InteractiveFormAttributeValueLocalizedModel> Locales { get; set; }

    }

    public partial class InteractiveFormAttributeValueLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Values.Fields.Name")]
        public string Name { get; set; }

    }

}