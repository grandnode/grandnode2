using Grand.Api.Constants;
using Grand.Api.Infrastructure.DependencyManagement;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.TypeSearchers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Linq;

namespace Grand.Api.Infrastructure
{
    public partial class ODataStartup : IStartupApplication
    {
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            var apiConfig = application.ApplicationServices.GetService<ApiConfig>();
            if (apiConfig.Enabled)
            {
                application.UseCors(Configurations.CorsPolicyName);
            }
        }

        public void ConfigureServices(IServiceCollection services,
            IConfiguration configuration)
        {
            var apiConfig = services.BuildServiceProvider().GetService<ApiConfig>();
            if (apiConfig.Enabled)
            {
                //cors
                services.AddCors(options =>
                {
                    options.AddPolicy(Configurations.CorsPolicyName,
                        builder => builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
                });
                //Add OData
                services.AddControllers().AddOData(opt =>
                {
                    opt.EnableQueryFeatures(Configurations.MaxLimit);
                    opt.AddRouteComponents(Configurations.ODataRoutePrefix, GetEdmModel(apiConfig));
                    opt.Select().Filter().Count().Expand();
                });
            }
        }
        public int Priority => 505;
        public bool BeforeConfigure => false;


        private IEdmModel GetEdmModel(ApiConfig apiConfig)
        {
            var builder = new ODataConventionModelBuilder {
                Namespace = Configurations.ODataModelBuilderNamespace
            };
            RegisterDependencies(builder, apiConfig);
            return builder.GetEdmModel();
        }

        private void RegisterDependencies(ODataConventionModelBuilder builder, ApiConfig apiConfig)
        {
            var typeFinder = new AppTypeSearcher();

            //find dependency provided by other assemblies
            var dependencyInject = typeFinder.ClassesOfType<IDependencyEdmModel>();

            //create and sort instances of dependency inject
            var instances = dependencyInject
                .Select(di => (IDependencyEdmModel)Activator.CreateInstance(di))
                .OrderBy(di => di.Order);

            //register all provided dependencies
            foreach (var dependencyRegistrar in instances)
                dependencyRegistrar.Register(builder, apiConfig);

        }
    }
}
