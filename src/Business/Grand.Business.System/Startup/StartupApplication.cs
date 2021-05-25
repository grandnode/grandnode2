using Grand.Business.System.Interfaces.Admin;
using Grand.Business.System.Interfaces.ExportImport;
using Grand.Business.System.Interfaces.Installation;
using Grand.Business.System.Interfaces.MachineNameProvider;
using Grand.Business.System.Interfaces.Reports;
using Grand.Business.System.Interfaces.ScheduleTasks;
using Grand.Business.System.Services.Admin;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Grand.Business.System.Services.ExportImport;
using Grand.Business.System.Services.Installation;
using Grand.Business.System.Services.MachineNameProvider;
using Grand.Business.System.Services.Reports;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var config = new AppConfig();
            configuration.GetSection("Application").Bind(config);

            RegisterReports(services);
            RegisterMachineNameProvider(services, config);
            RegisterTask(services);
            RegisterExportImportService(services);
            RegisterInstallService(services);
            RegisterAdmin(services);
        }
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;

        private void RegisterTask(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IScheduleTaskService, ScheduleTaskService>();

            serviceCollection.AddScoped<IScheduleTask, QueuedMessagesSendScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, ClearCacheScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, ClearLogScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, GenerateSitemapXmlTask>();
            serviceCollection.AddScoped<IScheduleTask, CustomerReminderAbandonedCartScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, CustomerReminderBirthdayScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, CustomerReminderCompletedOrderScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, CustomerReminderLastActivityScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, CustomerReminderLastPurchaseScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, CustomerReminderRegisteredCustomerScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, CustomerReminderUnpaidOrderScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, DeleteGuestsScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, UpdateExchangeRateScheduleTask>();
            serviceCollection.AddScoped<IScheduleTask, EndAuctionsTask>();
            serviceCollection.AddScoped<IScheduleTask, CancelOrderScheduledTask>();

        }

        private void RegisterReports(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ICustomerReportService, CustomerReportService>();
            serviceCollection.AddScoped<IOrderReportService, OrderReportService>();
            serviceCollection.AddScoped<IProductsReportService, ProductsReportService>();
        }

        private void RegisterMachineNameProvider(IServiceCollection serviceCollection, AppConfig config)
        {
            if (config.RunOnAzureWebApps)
            {
                serviceCollection.AddSingleton<IMachineNameProvider, AzureWebAppsMachineNameProvider>();
            }
            else
            {
                serviceCollection.AddSingleton<IMachineNameProvider, DefaultMachineNameProvider>();
            }
        }

        private void RegisterInstallService(IServiceCollection serviceCollection)
        {
            var databaseInstalled = DataSettingsManager.DatabaseIsInstalled();
            if (!databaseInstalled)
            {
                //installation service
                serviceCollection.AddScoped<IInstallationLocalizedService, InstallationLocalizedService>();
                serviceCollection.AddScoped<IInstallationService, InstallationService>();
            }
        }

        private void RegisterExportImportService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IExportManager, ExportManager>();
            serviceCollection.AddScoped<IImportManager, ImportManager>();
        }
        private void RegisterAdmin(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IAdminSiteMapService, AdminSiteMapService>();
        }
    }
}
