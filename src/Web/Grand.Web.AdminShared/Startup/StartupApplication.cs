using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.Drivers.FileSystem.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.View;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.AdminShared.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        #region elFinder

        services.AddElFinderAspNetCore().AddFileSystemDriver();

        #endregion

        services.AddScoped<IAddressAttributeViewModelService, AddressAttributeViewModelService>();
        services.AddScoped<IAffiliateViewModelService, AffiliateViewModelService>();
        services.AddScoped<IBlogViewModelService, BlogViewModelService>();
        services.AddScoped<ICampaignViewModelService, CampaignViewModelService>();
        services.AddScoped<ICategoryViewModelService, CategoryViewModelService>();
        services.AddScoped<ICheckoutAttributeViewModelService, CheckoutAttributeViewModelService>();
        services.AddScoped<IContactAttributeViewModelService, ContactAttributeViewModelService>();
        services.AddScoped<IContactFormViewModelService, ContactFormViewModelService>();
        services.AddScoped<ICountryViewModelService, CountryViewModelService>();
        services.AddScoped<ICourseViewModelService, CourseViewModelService>();
        services.AddScoped<ICurrencyViewModelService, CurrencyViewModelService>();
        services.AddScoped<ICustomerAttributeViewModelService, CustomerAttributeViewModelService>();
        services.AddScoped<ICustomerViewModelService, CustomerViewModelService>();
        services.AddScoped<ICustomerReportViewModelService, CustomerReportViewModelService>();
        services.AddScoped<ICustomerGroupViewModelService, CustomerGroupViewModelService>();
        services.AddScoped<ICustomerTagViewModelService, CustomerTagViewModelService>();
        services.AddScoped<IDiscountViewModelService, DiscountViewModelService>();
        services.AddScoped<IDocumentViewModelService, DocumentViewModelService>();
        services.AddScoped<IEmailAccountViewModelService, EmailAccountViewModelService>();
        services.AddScoped<IGiftVoucherViewModelService, GiftVoucherViewModelService>();
        services.AddScoped<IKnowledgebaseViewModelService, KnowledgebaseViewModelService>();
        services.AddScoped<ILanguageViewModelService, LanguageViewModelService>();
        services.AddScoped<ICollectionViewModelService, CollectionViewModelService>();
        services.AddScoped<INewsViewModelService, NewsViewModelService>();
        services.AddScoped<IOrderViewModelService, OrderViewModelService>();
        services.AddScoped<IShipmentViewModelService, ShipmentViewModelService>();
        services.AddScoped<IProductReviewViewModelService, ProductReviewViewModelService>();
        services.AddScoped<IMerchandiseReturnViewModelService, MerchandiseReturnViewModelService>();
        services.AddScoped<IVendorViewModelService, VendorViewModelService>();
        services.AddScoped<IPageViewModelService, PageViewModelService>();
        services.AddScoped<IStoreViewModelService, StoreViewModelService>();
        services.AddScoped<IBrandViewModelService, BrandViewModelService>();
        services.AddScoped<IProductViewModelService, ProductViewModelService>();
        services.AddScoped<IPictureViewModelService, PictureViewModelService>();
        services.AddScoped<IElFinderViewModelService, ElFinderViewModelService>();
        services.AddScoped<IMenuViewModelService, MenuViewModelService>();

        // IAdminDataScope<Product>: registered once here (not per-host) via a route-driven resolver.
        // Grand.Web (the combined host) references Admin, Store, and Vendor together in one DI
        // container, so three competing AddScoped<IAdminDataScope<Product>, X>() calls (one per host's
        // own StartupApplication) would just have the last-registered host silently win for every
        // area - see RoutedProductDataScope's doc comment for the NullReferenceException this caused.
        // The three concrete scopes are registered as themselves so the resolver can pick between them
        // per-request based on the "area" route value.
        services.AddScoped<GlobalAdminDataScope<Product>>();
        services.AddScoped<StoreAdminDataScope<Product>>();
        services.AddScoped<VendorProductDataScope>();
        services.AddScoped<IAdminDataScope<Product>, RoutedProductDataScope>();

        // IAdminDataScope<Category>: registered once here for the same reason as Product above — see
        // RoutedCategoryDataScope's doc comment. No Vendor scope: Category has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<Category>>();
        services.AddScoped<StoreAdminDataScope<Category>>();
        services.AddScoped<IAdminDataScope<Category>, RoutedCategoryDataScope>();

        // IAdminDataScope<Collection>: registered once here for the same reason as Category above — see
        // RoutedCollectionDataScope's doc comment. No Vendor scope: Collection has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<Collection>>();
        services.AddScoped<StoreAdminDataScope<Collection>>();
        services.AddScoped<IAdminDataScope<Collection>, RoutedCollectionDataScope>();

        // IAdminDataScope<Order>: three bespoke implementations, none reusing the generic Global/Store
        // scopes — see AdminOrderDataScope/StoreOrderDataScope/VendorOrderDataScope doc comments.
        services.AddScoped<AdminOrderDataScope>();
        services.AddScoped<StoreOrderDataScope>();
        services.AddScoped<VendorOrderDataScope>();
        services.AddScoped<IAdminDataScope<Order>, RoutedOrderDataScope>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 101;
    public bool BeforeConfigure => false;
}