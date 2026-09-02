using elFinder.Net.AspNetCore.Extensions;
using elFinder.Net.Drivers.FileSystem.Extensions;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Vendors;
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

        // IAdminDataScope<Brand>: registered once here for the same reason as Category/Collection above —
        // see RoutedBrandDataScope's doc comment. No Vendor scope: Brand has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<Brand>>();
        services.AddScoped<StoreAdminDataScope<Brand>>();
        services.AddScoped<IAdminDataScope<Brand>, RoutedBrandDataScope>();

        // IAdminDataScope<Order>: three bespoke implementations, none reusing the generic Global/Store
        // scopes — see AdminOrderDataScope/StoreOrderDataScope/VendorOrderDataScope doc comments.
        services.AddScoped<AdminOrderDataScope>();
        services.AddScoped<StoreOrderDataScope>();
        services.AddScoped<VendorOrderDataScope>();
        services.AddScoped<IAdminDataScope<Order>, RoutedOrderDataScope>();

        // IAdminDataScope<Shipment>: registered once here for the same reason as Order above — see
        // RoutedShipmentDataScope's doc comment. Admin reuses the generic GlobalAdminDataScope<T>
        // unmodified (no Sales-Manager restriction on Shipment); Store/Vendor are bespoke because
        // Shipment isn't IStoreLinkEntity and Vendor ownership is a flat VendorId field.
        services.AddScoped<GlobalAdminDataScope<Shipment>>();
        services.AddScoped<StoreShipmentDataScope>();
        services.AddScoped<VendorShipmentDataScope>();
        services.AddScoped<IAdminDataScope<Shipment>, RoutedShipmentDataScope>();

        // IAdminDataScope<PaymentTransaction>: registered once here for the same reason as
        // Product/Category/Collection/Order above — see RoutedPaymentTransactionDataScope's doc
        // comment. No Vendor scope: PaymentTransaction has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<PaymentTransaction>>();
        services.AddScoped<StorePaymentTransactionDataScope>();
        services.AddScoped<IAdminDataScope<PaymentTransaction>, RoutedPaymentTransactionDataScope>();

        // IAdminDataScope<MerchandiseReturn>: Admin reuses the generic GlobalAdminDataScope directly (no
        // bespoke Admin scope - confirmed no restriction exists despite the entity's SeId field, spec §2.1).
        services.AddScoped<GlobalAdminDataScope<MerchandiseReturn>>();
        services.AddScoped<StoreMerchandiseReturnDataScope>();
        services.AddScoped<VendorMerchandiseReturnDataScope>();
        services.AddScoped<IAdminDataScope<MerchandiseReturn>, RoutedMerchandiseReturnDataScope>();

        // IReportDataScope: NOT an IAdminDataScope<TEntity> registration (Reports has no entity —
        // see IReportDataScope's doc comment and ARCH-001 Reports consolidation spec §3). All three
        // hosts have a Reports screen, so all three concrete scopes are registered.
        services.AddScoped<AdminReportDataScope>();
        services.AddScoped<StoreReportDataScope>();
        services.AddScoped<VendorReportDataScope>();
        services.AddScoped<IReportDataScope, RoutedReportDataScope>();

        // IAdminDataScope<VendorReview>: registered once here for the same reason as
        // Product/Category/Collection/Order/Shipment/PaymentTransaction above — see
        // RoutedVendorReviewDataScope's doc comment. No Store scope: VendorReview has no Store
        // screen. Admin reuses the generic GlobalAdminDataScope<T> unmodified (no restriction on
        // Admin's original controller).
        services.AddScoped<GlobalAdminDataScope<VendorReview>>();
        services.AddScoped<VendorVendorReviewDataScope>();
        services.AddScoped<IAdminDataScope<VendorReview>, RoutedVendorReviewDataScope>();

        // IAdminDataScope<AddressAttribute>: registered once here for the same reason as
        // Product/Category/Collection above — see RoutedAddressAttributeDataScope's doc comment.
        // No Vendor scope: AddressAttribute has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<AddressAttribute>>();
        services.AddScoped<StoreAdminDataScope<AddressAttribute>>();
        services.AddScoped<IAdminDataScope<AddressAttribute>, RoutedAddressAttributeDataScope>();

        // IAdminDataScope<ContactAttribute>: registered once here for the same reason as
        // AddressAttribute above — see RoutedContactAttributeDataScope's doc comment.
        // No Vendor scope: ContactAttribute has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<ContactAttribute>>();
        services.AddScoped<StoreAdminDataScope<ContactAttribute>>();
        services.AddScoped<IAdminDataScope<ContactAttribute>, RoutedContactAttributeDataScope>();

        // IAdminDataScope<CustomerAttribute>: registered once here for the same reason as
        // AddressAttribute above — see RoutedCustomerAttributeDataScope's doc comment.
        // No Vendor scope: CustomerAttribute has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<CustomerAttribute>>();
        services.AddScoped<StoreAdminDataScope<CustomerAttribute>>();
        services.AddScoped<IAdminDataScope<CustomerAttribute>, RoutedCustomerAttributeDataScope>();

        // IAdminDataScope<CheckoutAttribute>: registered once here for the same reason as
        // AddressAttribute above — see RoutedCheckoutAttributeDataScope's doc comment.
        // No Vendor scope: CheckoutAttribute has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<CheckoutAttribute>>();
        services.AddScoped<StoreAdminDataScope<CheckoutAttribute>>();
        services.AddScoped<IAdminDataScope<CheckoutAttribute>, RoutedCheckoutAttributeDataScope>();

        // IAdminDataScope<ProductAttribute>: registered once here for the same reason as
        // AddressAttribute above — see RoutedProductAttributeDataScope's doc comment.
        // No Vendor scope: ProductAttribute has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<ProductAttribute>>();
        services.AddScoped<StoreAdminDataScope<ProductAttribute>>();
        services.AddScoped<IAdminDataScope<ProductAttribute>, RoutedProductAttributeDataScope>();

        // IAdminDataScope<SpecificationAttribute>: registered once here for the same reason as
        // AddressAttribute above — see RoutedSpecificationAttributeDataScope's doc comment.
        // No Vendor scope: SpecificationAttribute has no Vendor screen.
        services.AddScoped<GlobalAdminDataScope<SpecificationAttribute>>();
        services.AddScoped<StoreAdminDataScope<SpecificationAttribute>>();
        services.AddScoped<IAdminDataScope<SpecificationAttribute>, RoutedSpecificationAttributeDataScope>();

        // IAdminDataScope<BlogPost>/<BlogCategory>: registered once here for the same reason as
        // Category above — see RoutedBlogPostDataScope's doc comment. No Vendor scope: Blog has no
        // Vendor screen. Two entities share one controller (BaseBlogController), so two routed
        // scopes are registered.
        services.AddScoped<GlobalAdminDataScope<BlogPost>>();
        services.AddScoped<StoreAdminDataScope<BlogPost>>();
        services.AddScoped<IAdminDataScope<BlogPost>, RoutedBlogPostDataScope>();

        services.AddScoped<GlobalAdminDataScope<BlogCategory>>();
        services.AddScoped<StoreAdminDataScope<BlogCategory>>();
        services.AddScoped<IAdminDataScope<BlogCategory>, RoutedBlogCategoryDataScope>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 101;
    public bool BeforeConfigure => false;
}