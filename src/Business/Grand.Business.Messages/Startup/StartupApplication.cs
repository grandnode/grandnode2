using Grand.Business.Messages.Interfaces;
using Grand.Business.Messages.Services;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Messages.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IMessageTemplateService, MessageTemplateService>();
            services.AddScoped<IQueuedEmailService, QueuedEmailService>();
            services.AddScoped<IEmailAccountService, EmailAccountService>();
            services.AddScoped<IMessageProviderService, MessageProviderService>();
            services.AddScoped<IMessageTokenProvider, MessageTokenProvider>();            
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IMimeMappingService, MimeMappingService>(x =>
            {
                var provider = new FileExtensionContentTypeProvider();
                return new MimeMappingService(provider);
            });
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;

        public bool BeforeConfigure => false;
    }
}
