using Grand.Data;
using Grand.Domain;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Courses;
using Grand.Domain.Customers;
using Grand.Domain.Knowledgebase;
using Grand.Domain.News;
using Grand.Domain.Pages;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Migrations;
using Grand.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Sanitizes rich text that was stored before sanitization was applied on write.
///     Sanitizing on render covers the Razor views, but the storefront also serializes these fields into the
///     view-model json island, where the Vue layer binds them with v-html - that path only becomes safe once the
///     stored value itself is clean. Running once here is also what makes the api responses safe.
/// </summary>
public class MigrationSanitizeStoredRichText : IMigration
{
    public int Priority => 3;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("B7E14C29-6A83-4F55-9D07-2E48A1C6F390");
    public string Name => "Sanitize rich text stored before sanitization was applied on write";

    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var sanitizer = serviceProvider.GetRequiredService<IHtmlSanitizationService>();
        var logger = serviceProvider.GetRequiredService<ILogger<MigrationSanitizeStoredRichText>>();

        try
        {
            var changed = 0;

            changed += Sanitize<Product>(serviceProvider, sanitizer,
                (x, s) => {
                    x.ShortDescription = s(x.ShortDescription);
                    x.FullDescription = s(x.FullDescription);
                });

            changed += Sanitize<Category>(serviceProvider, sanitizer,
                (x, s) => {
                    x.Description = s(x.Description);
                    x.BottomDescription = s(x.BottomDescription);
                });

            changed += Sanitize<Brand>(serviceProvider, sanitizer,
                (x, s) => {
                    x.Description = s(x.Description);
                    x.BottomDescription = s(x.BottomDescription);
                });

            changed += Sanitize<Collection>(serviceProvider, sanitizer,
                (x, s) => {
                    x.Description = s(x.Description);
                    x.BottomDescription = s(x.BottomDescription);
                });

            changed += Sanitize<BlogPost>(serviceProvider, sanitizer,
                (x, s) => {
                    x.Body = s(x.Body);
                    x.BodyOverview = s(x.BodyOverview);
                });

            changed += Sanitize<NewsItem>(serviceProvider, sanitizer,
                (x, s) => {
                    x.Short = s(x.Short);
                    x.Full = s(x.Full);
                });

            changed += Sanitize<Page>(serviceProvider, sanitizer, (x, s) => x.Body = s(x.Body));
            changed += Sanitize<Course>(serviceProvider, sanitizer, (x, s) => x.Description = s(x.Description));
            changed += Sanitize<CourseLesson>(serviceProvider, sanitizer,
                (x, s) => x.Description = s(x.Description));
            changed += Sanitize<KnowledgebaseArticle>(serviceProvider, sanitizer,
                (x, s) => x.Content = s(x.Content));
            changed += Sanitize<KnowledgebaseCategory>(serviceProvider, sanitizer,
                (x, s) => x.Description = s(x.Description));
            changed += Sanitize<Vendor>(serviceProvider, sanitizer,
                (x, s) => x.Description = s(x.Description));

            logger.LogInformation("Sanitized stored rich text on {Count} record(s)", changed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UpgradeProcess - SanitizeStoredRichText (2.4)");
        }

        return true;
    }

    /// <summary>
    ///     Rewrites only the records the sanitizer actually changed, so an installation with clean content is not
    ///     rewritten wholesale.
    /// </summary>
    private static int Sanitize<T>(IServiceProvider serviceProvider, IHtmlSanitizationService sanitizer,
        Action<T, Func<string, string>> apply) where T : BaseEntity
    {
        var repository = serviceProvider.GetRequiredService<IRepository<T>>();
        var changed = 0;

        foreach (var entity in repository.Table.ToList())
        {
            var dirty = false;

            apply(entity, value => {
                var sanitized = sanitizer.SanitizeRichText(value);
                if (!string.Equals(sanitized, value, StringComparison.Ordinal)) dirty = true;
                return sanitized;
            });

            if (!dirty) continue;

            repository.Update(entity);
            changed++;
        }

        return changed;
    }
}
