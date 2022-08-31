using Grand.Business.Core.Interfaces.System.Admin;
using Grand.Business.Core.Interfaces.System.ExportImport;
using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Business.Core.Interfaces.System.MachineNameProvider;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Business.System.Services.Admin;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Grand.Business.System.Services.ExportImport;
using Grand.Business.System.Services.Installation;
using Grand.Business.System.Services.MachineNameProvider;
using Grand.Business.System.Services.Migrations;
using Grand.Business.System.Services.Reports;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Migrations;
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
            RegisterReports(services);
            RegisterMachineNameProvider(services, configuration);
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

        private void RegisterMachineNameProvider(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var config = new AzureConfig();
            configuration.GetSection("Azure").Bind(config);
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

            serviceCollection.AddScoped<IMigrationProcess, MigrationProcess>();
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
