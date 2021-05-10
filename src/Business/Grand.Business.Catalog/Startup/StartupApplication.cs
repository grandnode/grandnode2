using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Catalog.Services.Categories;
using Grand.Business.Catalog.Services.Discounts;
using Grand.Business.Catalog.Services.Collections;
using Grand.Business.Catalog.Services.Prices;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Catalog.Services.Tax;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Grand.Business.Catalog.Services.Brands;
using Grand.Business.Catalog.Interfaces.Brands;

namespace Grand.Business.Catalog.Startup
{
    public class StartupApplication : IStartupApplication
    {

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            RegisterCatalogService(services);
            RegisterDiscountsService(services);
            RegisterTaxService(services);
        }

        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }
        public int Priority => 100;
        public bool BeforeConfigure => false;

        private void RegisterCatalogService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IOutOfStockSubscriptionService, OutOfStockSubscriptionService>();
            serviceCollection.AddScoped<ICategoryService, CategoryService>();
            serviceCollection.AddScoped<IBrandService, BrandService>();
            serviceCollection.AddScoped<ICompareProductsService, CompareProductsService>();
            serviceCollection.AddScoped<IRecentlyViewedProductsService, RecentlyViewedProductsService>();
            serviceCollection.AddScoped<ICollectionService, CollectionService>();
            serviceCollection.AddScoped<IPriceFormatter, PriceFormatter>();
            serviceCollection.AddScoped<IProductAttributeFormatter, ProductAttributeFormatter>();
            serviceCollection.AddScoped<IProductAttributeParser, ProductAttributeParser>();
            serviceCollection.AddScoped<IProductAttributeService, ProductAttributeService>();
            serviceCollection.AddScoped<IProductService, ProductService>();
            serviceCollection.AddScoped<IProductCategoryService, ProductCategoryService>();
            serviceCollection.AddScoped<IProductCollectionService, ProductCollectionService>();
            serviceCollection.AddScoped<IProductReviewService, ProductReviewService>();
            serviceCollection.AddScoped<ICopyProductService, CopyProductService>();
            serviceCollection.AddScoped<IProductReservationService, ProductReservationService>();
            serviceCollection.AddScoped<IAuctionService, AuctionService>();
            serviceCollection.AddScoped<IProductCourseService, ProductCourseService>();
            serviceCollection.AddScoped<ISpecificationAttributeService, SpecificationAttributeService>();
            serviceCollection.AddScoped<IProductLayoutService, ProductLayoutService>();
            serviceCollection.AddScoped<IBrandLayoutService, BrandLayoutService>();
            serviceCollection.AddScoped<ICategoryLayoutService, CategoryLayoutService>();
            serviceCollection.AddScoped<ICollectionLayoutService, CollectionLayoutService>();
            serviceCollection.AddScoped<IProductTagService, ProductTagService>();
            serviceCollection.AddScoped<ICustomerGroupProductService, CustomerGroupProductService>();
            serviceCollection.AddScoped<IInventoryManageService, InventoryManageService>();
            serviceCollection.AddScoped<IStockQuantityService, StockQuantityService>();
            serviceCollection.AddScoped<IPricingService, PricingService>();
        }

        private void RegisterDiscountsService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IDiscountService, DiscountService>();
        }

        private void RegisterTaxService(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ITaxService, TaxService>();
            serviceCollection.AddScoped<IVatService, VatService>();
            serviceCollection.AddScoped<ITaxCategoryService, TaxCategoryService>();
        }
    }
}
