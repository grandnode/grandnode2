using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class InteractiveFormAttributeModel : BaseEntityModel, ILocalizedModel<InteractiveFormAttributeLocalizedModel>
    {
        public InteractiveFormAttributeModel()
        {
            Locales = new List<InteractiveFormAttributeLocalizedModel>();
        }
        public string FormId { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.SystemName")]
        public string SystemName { get; set; }


        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.RegexValidation")]
        public string RegexValidation { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.IsRequired")]
        public bool IsRequired { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.FormControlTypeId")]
        public int FormControlTypeId { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.ValidationMinLength")]
        [UIHint("Int32Nullable")]
        public int? ValidationMinLength { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.ValidationMaxLength")]
        [UIHint("Int32Nullable")]
        public int? ValidationMaxLength { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.DefaultValue")]
        public string DefaultValue { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.Style")]
        public string Style { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.Class")]
        public string Class { get; set; }

        public IList<InteractiveFormAttributeLocalizedModel> Locales { get; set; }

    }

    public partial class InteractiveFormAttributeLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Attribute.Fields.Name")]
        public string Name { get; set; }

    }

}