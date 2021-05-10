using Grand.Business.Cms.Interfaces;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using MongoDB.Bson.Serialization;
using Widgets.Slider.Domain;
using Widgets.Slider.Services;

namespace Widgets.Slider
{
    public class StartupApplication : IStartupApplication
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IWidgetProvider, SliderWidgetProvider>();

            BsonClassMap.RegisterClassMap<PictureSlider>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(c => c.SliderType);
            });
            services.AddScoped<ISliderService, SliderService>();

            services.Configure<MvcRazorRuntimeCompilationOptions>(options =>
            {
                options.FileProviders.Add(
                    new EmbeddedFileProvider(typeof(SliderWidgetPlugin).Assembly));
            });
        }

        public int Priority => 10;
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public bool BeforeConfigure => false;
    }

}
