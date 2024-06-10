using Grand.Business.Catalog.Services.Brands;
using Grand.Business.Catalog.Services.Categories;
using Grand.Business.Catalog.Services.Collections;
using Grand.Business.Catalog.Services.Directory;
using Grand.Business.Catalog.Services.Discounts;
using Grand.Business.Catalog.Services.ExportImport;
using Grand.Business.Catalog.Services.Prices;
using Grand.Business.Catalog.Services.Products;
using Grand.Business.Catalog.Services.Tax;
using Grand.Business.Core.Dto;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.Catalog.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterCatalogService(services);
        RegisterDiscountsService(services);
        RegisterTaxService(services);
        RegisterExportImport(services);
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
        serviceCollection.AddScoped<IRecentlyViewedProductsService, RecentlyViewedProductsService>();
        serviceCollection.AddScoped<ICollectionService, CollectionService>();
        serviceCollection.AddScoped<IPriceFormatter, PriceFormatter>();
        serviceCollection.AddScoped<IProductAttributeFormatter, ProductAttributeFormatter>();
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
        serviceCollection.AddScoped<ISearchTermService, SearchTermService>();
        serviceCollection.AddScoped<IGeoLookupService, GeoLookupService>();
        serviceCollection.AddScoped<IMeasureService, MeasureService>();
    }

    private void RegisterDiscountsService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IDiscountService, DiscountService>();
        serviceCollection.AddScoped<IDiscountValidationService, DiscountValidationService>();
        serviceCollection.AddScoped<IDiscountProviderLoader, DiscountProviderLoader>();
    }

    private void RegisterTaxService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ITaxService, TaxService>();
        serviceCollection.AddScoped<IVatService, VatService>();
        serviceCollection.AddScoped<ITaxCategoryService, TaxCategoryService>();
    }

    private void RegisterExportImport(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<ISchemaProperty<Brand>, BrandSchemaProperty>();
        serviceCollection.AddScoped<ISchemaProperty<Category>, CategorySchemaProperty>();
        serviceCollection.AddScoped<ISchemaProperty<Collection>, CollectionSchemaProperty>();
        serviceCollection.AddScoped<ISchemaProperty<Product>, ProductSchemaProperty>();

        serviceCollection.AddScoped<IImportDataObject<CategoryDto>, CategoryImportDataObject>();
        serviceCollection.AddScoped<IImportDataObject<BrandDto>, BrandImportDataObject>();
        serviceCollection.AddScoped<IImportDataObject<CollectionDto>, CollectionImportDataObject>();
        serviceCollection.AddScoped<IImportDataObject<ProductDto>, ProductImportDataObject>();
    }
}