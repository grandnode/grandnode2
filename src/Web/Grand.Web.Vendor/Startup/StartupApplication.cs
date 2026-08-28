using Grand.Data;
using Grand.Infrastructure;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Services;

namespace Grand.Web.Vendor.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        // IAdminDataScope<Product> is registered once, centrally, by Grand.Web.AdminShared's own
        // StartupApplication via RoutedProductDataScope - see its doc comment. A plain
        // AddScoped<IAdminDataScope<Product>, VendorProductDataScope>() here used to win for every
        // area under the combined Grand.Web host (Vendor's StartupApplication has the highest
        // Priority, so its registration ran last and silently overrode Admin's/Store's), causing a
        // NullReferenceException in VendorProductDataScope.DefaultVendorId whenever an Admin/Store
        // user opened the product list under that host.
        // IProductViewModelService is registered by Grand.Web.AdminShared's own StartupApplication
        // (Grand.Web.AdminShared/Startup/StartupApplication.cs), which is discovered and run for this
        // host too via the IStartupApplication assembly scan in StartupBase, since Vendor references
        // AdminShared. Registering it again here would just be a redundant duplicate of that line.
        // IOrderViewModelService is likewise registered by Grand.Web.AdminShared's StartupApplication.
        // IShipmentViewModelService is likewise registered by Grand.Web.AdminShared's StartupApplication.
        // IMerchandiseReturnViewModelService is likewise registered by Grand.Web.AdminShared's
        // StartupApplication (Vendor's own IMerchandiseReturnViewModelService/
        // MerchandiseReturnViewModelService were deleted as part of ARCH-001 MerchandiseReturn
        // consolidation - this host now consumes the shared AdminShared service).
        services.AddScoped<IVendorReviewViewModelService, VendorReviewViewModelService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 102;
    public bool BeforeConfigure => false;
}