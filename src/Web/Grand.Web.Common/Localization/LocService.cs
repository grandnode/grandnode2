using Grand.Business.Common.Interfaces.Localization;
using Microsoft.Extensions.Localization;

namespace Grand.Web.Common.Localization
{
    public class LocService : StringLocalizer<string>
    {
        private readonly ITranslationService _translationService;
       
        public LocService(IStringLocalizerFactory factory) : base(factory)
        {

        }
        public LocService(IStringLocalizerFactory factory, ITranslationService translationService) : base(factory)
        {
            _translationService = translationService;
        }

        public override LocalizedString this[string name]
        {
            get
            {
                var resFormat = _translationService.GetResource(name.ToLowerInvariant());
                return new LocalizedString(name, resFormat ?? name, resFormat == null);
            }
        }

        public override LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var resFormat = _translationService.GetResource(name.ToLowerInvariant());
                if (string.IsNullOrEmpty(resFormat))
                {
                    return new LocalizedString(name, resFormat, resFormat == null);
                }
                try
                {
                    return new LocalizedString(name, string.Format(resFormat, arguments),
                        resFormat == null);
                }
                catch
                {
                    return new LocalizedString(name, resFormat, resFormat == null);
                }
            }
        }
    }
}
