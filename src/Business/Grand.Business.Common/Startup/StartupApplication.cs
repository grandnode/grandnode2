using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Pdf;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Addresses;
using Grand.Business.Common.Services.Configuration;
using Grand.Business.Common.Services.Directory;
using Grand.Business.Common.Services.Localization;
using Grand.Business.Common.Services.Logging;
using Grand.Business.Common.Services.Pdf;
using Grand.Business.Common.Services.Security;
using Grand.Business.Common.Services.Seo;
using Grand.Business.Common.Services.Stores;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Grand.Business.Common.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            RegisterCommonService(services);
            RegisterDirectoryService(services);
            RegisterConfigurationService(services);
            RegisterLocalizationService(services);
            RegisterLoggingService(services);
            RegisterSecurityService(services);
            RegisterSeoService(services);
            RegisterStoresService(services);
        }
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;

        private void RegisterCommonService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IAddressAttributeParser, AddressAttributeParser>();
            serviceCollection.AddScoped<IAddressAttributeService, AddressAttributeService>();
            serviceCollection.AddScoped<IUserFieldService, UserFieldService>();
            serviceCollection.AddScoped<IHistoryService, HistoryService>();
            serviceCollection.AddScoped<IPdfService, WkPdfService>();
            serviceCollection.AddScoped<IViewRenderService, ViewRenderService>();

        }
        private void RegisterDirectoryService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ISearchTermService, SearchTermService>();
            serviceCollection.AddScoped<IDateTimeService, DateTimeService>();
            serviceCollection.AddScoped<ICookiePreference, CookiePreference>();
            serviceCollection.AddScoped<IGeoLookupService, GeoLookupService>();
            serviceCollection.AddScoped<ICountryService, CountryService>();
            serviceCollection.AddScoped<ICurrencyService, CurrencyService>();
            serviceCollection.AddScoped<IExchangeRateService, ExchangeRateService>();
            serviceCollection.AddScoped<IMeasureService, MeasureService>();
            serviceCollection.AddScoped<IGroupService, GroupService>();
        }
        private void RegisterConfigurationService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ISettingService, SettingService>();
            serviceCollection.AddScoped<IGoogleAnalyticsService, GoogleAnalyticsService>();
        }
        private void RegisterLocalizationService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ITranslationService, TranslationService>();
            serviceCollection.AddScoped<ILanguageService, LanguageService>();
        }
        private void RegisterLoggingService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ICustomerActivityService, CustomerActivityService>();
            serviceCollection.AddScoped<IActivityKeywordsProvider, ActivityKeywordsProvider>();
            serviceCollection.AddScoped<ILogger, DefaultLogger>();

        }
        private void RegisterSecurityService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IPermissionService, PermissionService>();
            serviceCollection.AddScoped<IAclService, AclService>();
            serviceCollection.AddScoped<IEncryptionService, EncryptionService>();

        }

        private void RegisterSeoService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ISlugService, SlugService>();
        }


        private void RegisterStoresService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IStoreService, StoreService>();
        }
    }
}
