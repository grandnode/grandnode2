using Grand.Web.Common.Localization;
using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Messages
{
    public partial class InteractiveFormModel : BaseEntityModel, ILocalizedModel<InteractiveFormLocalizedModel>
    {
        public InteractiveFormModel()
        {
            Locales = new List<InteractiveFormLocalizedModel>();
            AvailableEmailAccounts = new List<EmailAccountModel>();
        }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Fields.SystemName")]
        public string SystemName { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Fields.Body")]
        public string Body { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Fields.EmailAccount")]
        public string EmailAccountId { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Fields.AvailableTokens")]
        public string AvailableTokens { get; set; }
        public IList<EmailAccountModel> AvailableEmailAccounts { get; set; }

        public IList<InteractiveFormLocalizedModel> Locales { get; set; }

    }

    public partial class InteractiveFormLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Fields.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("admin.marketing.InteractiveForms.Fields.Body")]

        public string Body { get; set; }

    }

}