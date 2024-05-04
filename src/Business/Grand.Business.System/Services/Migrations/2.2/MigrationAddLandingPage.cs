using Grand.Data;
using Grand.Domain.Pages;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._2._2;

public class MigrationAddLandingPage : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 2);
    public Guid Identity => new("0833f104-54d5-41d0-83da-375d2520f709");
    public string Name => "Add new landing page - VendorPortalInfo";

    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        var pageLayoutRepository = serviceProvider.GetRequiredService<IRepository<PageLayout>>();
        var defaultPageLayout =
            pageLayoutRepository.Table.FirstOrDefault(tt => tt.Name == "Default layout");

        var repository = serviceProvider.GetRequiredService<IRepository<Page>>();
        var page = new Page {
            SystemName = "VendorPortalInfo",
            IncludeInSitemap = false,
            IsPasswordProtected = false,
            DisplayOrder = 1,
            Title = "Welcome to our Vendor Management Hub!",
            Body =
                "<p>Manage your product catalog, oversee customer orders, and streamline your shipping processes. Your vendor dashboard is the command center for your success. Stay organized, serve your customers efficiently, and watch your business thrive.</p>",
            PageLayoutId = defaultPageLayout?.Id,
            Published = true
        };
        repository.Insert(page);
        return true;
    }
}