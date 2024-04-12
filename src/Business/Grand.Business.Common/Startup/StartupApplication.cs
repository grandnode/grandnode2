using Grand.Business.Common.Services.Addresses;
using Grand.Business.Common.Services.Configuration;
using Grand.Business.Common.Services.Directory;
using Grand.Business.Common.Services.ExportImport;
using Grand.Business.Common.Services.Localization;
using Grand.Business.Common.Services.Pdf;
using Grand.Business.Common.Services.Security;
using Grand.Business.Common.Services.Seo;
using Grand.Business.Common.Services.Stores;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Common.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterCommonService(services);
        RegisterDirectoryService(services);
        RegisterConfigurationService(services);
        RegisterLocalizationService(services);
        RegisterSecurityService(services);
        RegisterSeoService(services);
        RegisterStoresService(services);
        RegisterExportImportService(services);
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
        serviceCollection.AddScoped<IPdfService, HtmlToPdfService>();
    }

    private void RegisterDirectoryService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IDateTimeService, DateTimeService>();

        serviceCollection.AddScoped<ICountryService, CountryService>();
        serviceCollection.AddScoped<ICurrencyService, CurrencyService>();
        serviceCollection.AddScoped<IExchangeRateService, ExchangeRateService>();
        serviceCollection.AddScoped<IGroupService, GroupService>();
    }

    private void RegisterConfigurationService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISettingService, SettingService>();
    }

    private void RegisterLocalizationService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ITranslationService, TranslationService>();
        serviceCollection.AddScoped<ILanguageService, LanguageService>();
    }

    private void RegisterSecurityService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IPermissionService, PermissionService>();
        serviceCollection.AddScoped<IAclService, AclService>();
        serviceCollection.AddScoped<IEncryptionService, EncryptionService>();
        serviceCollection.AddScoped<IPermissionProvider, PermissionProvider>();
    }

    private void RegisterSeoService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISlugService, SlugService>();
    }


    private void RegisterStoresService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IStoreService, StoreService>();
    }

    private void RegisterExportImportService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISchemaProperty<CountryStatesDto>, CountrySchemaProperty>();

        serviceCollection.AddScoped<IExportProvider, ExcelExportProvider>();
        serviceCollection.AddScoped(typeof(IExportManager<>), typeof(ExportManager<>));

        serviceCollection.AddScoped<IImportDataProvider, ExcelImportProvider>();
        serviceCollection.AddScoped(typeof(IImportManager<>), typeof(ImportManager<>));

        serviceCollection.AddScoped<IImportDataObject<CountryStatesDto>, CountryImportDataObject>();
    }
}