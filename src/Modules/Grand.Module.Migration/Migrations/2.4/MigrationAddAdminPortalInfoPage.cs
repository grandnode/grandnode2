using Grand.Data;
using Grand.Domain.Pages;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Adds the AdminPortalInfo page - the welcome text the admin dashboard shows at its top, the
///     counterpart of StorePortalInfo and VendorPortalInfo. The installer seeds the same page for new
///     installations. The page is not published, so the storefront never serves it and it stays out of
///     the sitemap; the dashboard reads it by system name regardless.
///     Idempotent: an existing page with that system name (the operator may already have created or
///     edited one) is left untouched; only a missing slug is added to it.
/// </summary>
public class MigrationAddAdminPortalInfoPage : IMigration
{
    public const string PageSystemName = "AdminPortalInfo";

    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("736AE0EB-1899-408A-BE68-B14EA3BE0344");
    public string Name => "Add the AdminPortalInfo page shown on the admin dashboard 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationAddAdminPortalInfoPage>>();
        try
        {
            var pageRepository = serviceProvider.GetRequiredService<IRepository<Page>>();
            var pageLayoutRepository = serviceProvider.GetRequiredService<IRepository<PageLayout>>();
            var entityUrlRepository = serviceProvider.GetRequiredService<IRepository<EntityUrl>>();

            var systemName = PageSystemName.ToLowerInvariant();
            var page = pageRepository.Table.FirstOrDefault(x => x.SystemName.ToLower() == systemName);
            if (page == null)
            {
                var defaultPageLayout = pageLayoutRepository.Table.FirstOrDefault(x => x.Name == "Default layout");
                page = new Page {
                    SystemName = PageSystemName,
                    IncludeInSitemap = false,
                    IsPasswordProtected = false,
                    DisplayOrder = 1,
                    Title = "Welcome to your admin dashboard",
                    Body =
                        "<p>Follow today's orders, online visitors and active carts at a glance, see what needs your attention and jump straight into your daily work. You can edit or translate this message under Content &gt; Landing pages.</p>",
                    PageLayoutId = defaultPageLayout?.Id,
                    Published = false
                };
                pageRepository.Insert(page);
            }

            if (!entityUrlRepository.Table.Any(x => x.EntityId == page.Id && x.EntityName == "Page"))
            {
                var slug = UniqueSlug(entityUrlRepository, systemName);
                entityUrlRepository.Insert(new EntityUrl {
                    EntityId = page.Id,
                    EntityName = "Page",
                    LanguageId = "",
                    IsActive = true,
                    Slug = slug
                });
                if (string.IsNullOrEmpty(page.SeName))
                {
                    page.SeName = slug;
                    pageRepository.Update(page);
                }
            }

            //a dashboard rendered before the upgrade may have cached "no such page"
            var cache = serviceProvider.GetService<ICacheBase>();
            cache?.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - AddAdminPortalInfoPage (2.4)");
            return false;
        }

        return true;
    }

    private static string UniqueSlug(IRepository<EntityUrl> entityUrlRepository, string slug)
    {
        var candidate = slug;
        var suffix = 2;
        while (entityUrlRepository.Table.Any(x => x.Slug == candidate))
            candidate = $"{slug}-{suffix++}";
        return candidate;
    }
}
