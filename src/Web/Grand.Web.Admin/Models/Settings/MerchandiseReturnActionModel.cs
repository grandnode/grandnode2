﻿using Grand.Web.Common.Models;
using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;

namespace Grand.Web.Admin.Models.Settings
{
    public class MerchandiseReturnActionModel : BaseEntityModel, ILocalizedModel<MerchandiseReturnActionLocalizedModel>
    {
        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<MerchandiseReturnActionLocalizedModel> Locales { get; set; } = new List<MerchandiseReturnActionLocalizedModel>();
    }

    public class MerchandiseReturnActionLocalizedModel : ILocalizedModelLocal
    {
        public string LanguageId { get; set; }

        [GrandResourceDisplayName("Admin.Settings.Order.MerchandiseReturnActions.Name")]

        public string Name { get; set; }

    }
}