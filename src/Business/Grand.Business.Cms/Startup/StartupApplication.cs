using Grand.Business.Core.Interfaces.Cms;
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
            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<IWidgetService, WidgetService>();
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<IPageLayoutService, PageLayoutService>();
            services.AddScoped<IPageService, PageService>();
            services.AddScoped<ICookiePreference, CookiePreference>();
            services.AddScoped<IRobotsTxtService, RobotsTxtService>();

        }
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;


    }
}
