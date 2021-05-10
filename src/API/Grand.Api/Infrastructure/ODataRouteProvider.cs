using Grand.Api.Constants;
using Grand.Api.Infrastructure.DependencyManagement;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Endpoints;
using Grand.Infrastructure.TypeSearchers;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using System;
using System.Linq;

namespace Grand.Api.Infrastructure
{
    public class ODataRouteProvider : IEndpointProvider
    {
        public int Priority => 10;

        public void RegisterEndpoint(IEndpointRouteBuilder routeBuilder)
        {
            var apiConfig = routeBuilder.ServiceProvider.GetRequiredService<ApiConfig>();
            if (apiConfig.Enabled)
            {
                //OData
                var serviceProvider = routeBuilder.ServiceProvider;
                IEdmModel model = GetEdmModel(serviceProvider, apiConfig);
                routeBuilder.Count().Filter().OrderBy().MaxTop(Configurations.MaxLimit);
                routeBuilder.MapODataRoute(Configurations.ODataRouteName, Configurations.ODataRoutePrefix, model);
                routeBuilder.EnableDependencyInjection();
            }
        }

        private IEdmModel GetEdmModel(IServiceProvider serviceProvider, ApiConfig apiConfig)
        {
            var builder = new ODataConventionModelBuilder(serviceProvider);
            builder.Namespace = Configurations.ODataModelBuilderNamespace;
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
