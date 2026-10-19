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
    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationAddAdminPortalInfoPage>>();
        try
        {
            var pageRepository = serviceProvider.GetRequiredService<IRepository<Page>>();
            var pageLayoutRepository = serviceProvider.GetRequiredService<IRepository<PageLayout>>();
            var entityUrlRepository = serviceProvider.GetRequiredService<IRepository<EntityUrl>>();

            var systemName = PageSystemName.ToLowerInvariant();
            var page = await pageRepository.FirstOrDefaultAsync(pageRepository.Table.Where(x => x.SystemName.ToLower() == systemName));
            if (page == null)
            {
                var defaultPageLayout = await pageLayoutRepository.FirstOrDefaultAsync(pageLayoutRepository.Table.Where(x => x.Name == "Default layout"));
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
                await pageRepository.InsertAsync(page);
            }

            if (!await entityUrlRepository.AnyAsync(
                    entityUrlRepository.Table.Where(x => x.EntityId == page.Id && x.EntityName == "Page")))
            {
                var slug = await UniqueSlug(entityUrlRepository, systemName);
                await entityUrlRepository.InsertAsync(new EntityUrl {
                    EntityId = page.Id,
                    EntityName = "Page",
                    LanguageId = "",
                    IsActive = true,
                    Slug = slug
                });
                if (string.IsNullOrEmpty(page.SeName))
                {
                    page.SeName = slug;
                    await pageRepository.UpdateAsync(page);
                }
            }

            //a dashboard rendered before the upgrade may have cached "no such page"
            var cache = serviceProvider.GetService<ICacheBase>();
            if (cache != null)
                await cache.RemoveByPrefix(CacheKey.PAGES_PATTERN_KEY);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - AddAdminPortalInfoPage (2.4)");
            return false;
        }

        return true;
    }

    private static async Task<string> UniqueSlug(IRepository<EntityUrl> entityUrlRepository, string slug)
    {
        var candidate = slug;
        var suffix = 2;
        while (await entityUrlRepository.AnyAsync(entityUrlRepository.Table.Where(x => x.Slug == candidate)))
            candidate = $"{slug}-{suffix++}";
        return candidate;
    }
}
