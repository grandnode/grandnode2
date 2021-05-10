//Contribution https://github.com/smartstore/SmartStoreNET/blob/2.0.x/src/Presentation/SmartStore.Web.Framework/Localization/ILocalizedModel.cs

using System.Collections.Generic;

namespace Grand.Web.Common.Models
{
    public interface ILocalizedModel
    {

    }
    public interface ILocalizedModel<TLocalizedModel> : ILocalizedModel
    {
        IList<TLocalizedModel> Locales { get; set; }
    }
}
