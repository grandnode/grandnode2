using Grand.Api.Constants;
using Grand.Api.DTOs.Catalog;
using Grand.Api.DTOs.Common;
using Grand.Api.DTOs.Customers;
using Grand.Api.DTOs.Shipping;
using Grand.Api.Infrastructure.DependencyManagement;
using Grand.Api.Queries.Handlers.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.TypeSearchers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Grand.Api.Infrastructure
{
    public partial class ODataStartup : IStartupApplication
    {
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            var apiConfig = application.ApplicationServices.GetService<BackendAPIConfig>();
            if (apiConfig.Enabled)
            {
                application.UseCors(Configurations.CorsPolicyName);
            }
        }

        public void ConfigureServices(IServiceCollection services,
            IConfiguration configuration)
        {
            var apiConfig = services.BuildServiceProvider().GetService<BackendAPIConfig>();
            if (apiConfig.Enabled)
            {
                //register RequestHandler
                RegisterRequestHandler(services);

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


        private IEdmModel GetEdmModel(BackendAPIConfig apiConfig)
        {
            var builder = new ODataConventionModelBuilder {
                Namespace = Configurations.ODataModelBuilderNamespace
            };
            RegisterDependencies(builder, apiConfig);
            return builder.GetEdmModel();
        }

        private void RegisterDependencies(ODataConventionModelBuilder builder, BackendAPIConfig apiConfig)
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

        private void RegisterRequestHandler(IServiceCollection services)
        {

            //Workaround - there is a problem with register generic type with IRequestHandler

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<CountryDto, Domain.Directory.Country>,
                IQueryable<CountryDto>>), typeof(GetGenericQueryHandler<CountryDto, Domain.Directory.Country>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<CurrencyDto, Domain.Directory.Currency>,
                IQueryable<CurrencyDto>>), typeof(GetGenericQueryHandler<CurrencyDto, Domain.Directory.Currency>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<BrandDto, Domain.Catalog.Brand>,
                IQueryable<BrandDto>>), typeof(GetGenericQueryHandler<BrandDto, Domain.Catalog.Brand>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<CategoryDto, Domain.Catalog.Category>,
                IQueryable<CategoryDto>>), typeof(GetGenericQueryHandler<CategoryDto, Domain.Catalog.Category>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<CollectionDto, Domain.Catalog.Collection>,
                IQueryable<CollectionDto>>), typeof(GetGenericQueryHandler<CollectionDto, Domain.Catalog.Collection>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<ProductAttributeDto, Domain.Catalog.ProductAttribute>,
                IQueryable<ProductAttributeDto>>), typeof(GetGenericQueryHandler<ProductAttributeDto, Domain.Catalog.ProductAttribute>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<ProductDto, Domain.Catalog.Product>,
                IQueryable<ProductDto>>), typeof(GetGenericQueryHandler<ProductDto, Domain.Catalog.Product>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<SpecificationAttributeDto, Domain.Catalog.SpecificationAttribute>,
                IQueryable<SpecificationAttributeDto>>), typeof(GetGenericQueryHandler<SpecificationAttributeDto, Domain.Catalog.SpecificationAttribute>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<WarehouseDto, Domain.Shipping.Warehouse>,
                IQueryable<WarehouseDto>>), typeof(GetGenericQueryHandler<WarehouseDto, Domain.Shipping.Warehouse>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<ShippingMethodDto, Domain.Shipping.ShippingMethod>,
                IQueryable<ShippingMethodDto>>), typeof(GetGenericQueryHandler<ShippingMethodDto, Domain.Shipping.ShippingMethod>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<PickupPointDto, Domain.Shipping.PickupPoint>,
                IQueryable<PickupPointDto>>), typeof(GetGenericQueryHandler<PickupPointDto, Domain.Shipping.PickupPoint>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<DeliveryDateDto, Domain.Shipping.DeliveryDate>,
                IQueryable<DeliveryDateDto>>), typeof(GetGenericQueryHandler<DeliveryDateDto, Domain.Shipping.DeliveryDate>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<VendorDto, Domain.Vendors.Vendor>,
                IQueryable<VendorDto>>), typeof(GetGenericQueryHandler<VendorDto, Domain.Vendors.Vendor>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<CustomerGroupDto, Domain.Customers.CustomerGroup>,
                IQueryable<CustomerGroupDto>>), typeof(GetGenericQueryHandler<CustomerGroupDto, Domain.Customers.CustomerGroup>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<StoreDto, Domain.Stores.Store>,
                IQueryable<StoreDto>>), typeof(GetGenericQueryHandler<StoreDto, Domain.Stores.Store>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<LanguageDto, Domain.Localization.Language>,
                IQueryable<LanguageDto>>), typeof(GetGenericQueryHandler<LanguageDto, Domain.Localization.Language>));

            services.AddScoped(typeof(IRequestHandler<GetGenericQuery<PictureDto, Domain.Media.Picture>,
                IQueryable<PictureDto>>), typeof(GetGenericQueryHandler<PictureDto, Domain.Media.Picture>));

        }
    }
}
