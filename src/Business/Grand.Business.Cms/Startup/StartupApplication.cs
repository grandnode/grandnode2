using Grand.Business.Cms.Interfaces;
using Grand.Business.Cms.Services;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Cms.Startup
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            RegisterBlogService(services);
            RegisterWidgetService(services);
            RegisterNewsService(services);
            RegisterPagesService(services);
        }
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;

        private void RegisterBlogService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IBlogService, BlogService>();
        }

        private void RegisterWidgetService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IWidgetService, WidgetService>();
        }
        private void RegisterNewsService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<INewsService, NewsService>();
        }
        private void RegisterPagesService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IPageLayoutService, PageLayoutService>();
            serviceCollection.AddScoped<IPageService, PageService>();

        }

    }
}
