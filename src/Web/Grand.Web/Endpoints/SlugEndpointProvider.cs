using Grand.Data;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Endpoints;
using Grand.Web.Common.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Endpoints;

public class SlugEndpointProvider : IEndpointProvider
{
    public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        var pattern = "{**SeName}";
        var config = endpointRouteBuilder.ServiceProvider.GetRequiredService<AppConfig>();
        if (config.SeoFriendlyUrlsForLanguagesEnabled)
            pattern = $"{{language:lang={config.SeoFriendlyUrlsDefaultCode}}}/{{**SeName}}";

        endpointRouteBuilder.MapDynamicControllerRoute<SlugRouteTransformer>(pattern);

        //and default one
        endpointRouteBuilder.MapControllerRoute(
            "Default",
            "{controller=Home}/{action=Index}/{id?}");

        //generic URLs
        endpointRouteBuilder.MapControllerRoute(
            "GenericUrl",
            "{GenericSeName}",
            new { controller = "Common", action = "GenericUrl" });

        //define this routes to use in UI views (in case if you want to customize some of them later)
        endpointRouteBuilder.MapControllerRoute(
            "Product",
            pattern,
            new { controller = "Product", action = "ProductDetails" });

        endpointRouteBuilder.MapControllerRoute(
            "Category",
            pattern,
            new { controller = "Catalog", action = "Category" });

        endpointRouteBuilder.MapControllerRoute(
            "Brand",
            pattern,
            new { controller = "Catalog", action = "Brand" });

        endpointRouteBuilder.MapControllerRoute(
            "Collection",
            pattern,
            new { controller = "Catalog", action = "Collection" });

        endpointRouteBuilder.MapControllerRoute(
            "Vendor",
            pattern,
            new { controller = "Catalog", action = "Vendor" });

        endpointRouteBuilder.MapControllerRoute(
            "NewsItem",
            pattern,
            new { controller = "News", action = "NewsItem" });

        endpointRouteBuilder.MapControllerRoute(
            "BlogPost",
            pattern,
            new { controller = "Blog", action = "BlogPost" });

        endpointRouteBuilder.MapControllerRoute(
            "Page",
            pattern,
            new { controller = "Page", action = "PageDetails" });

        endpointRouteBuilder.MapControllerRoute(
            "KnowledgebaseArticle",
            pattern,
            new { controller = "Knowledgebase", action = "KnowledgebaseArticle" });

        endpointRouteBuilder.MapControllerRoute(
            "KnowledgebaseCategory",
            pattern,
            new { controller = "Knowledgebase", action = "ArticlesByCategory" });

        endpointRouteBuilder.MapControllerRoute(
            "Course",
            pattern,
            new { controller = "Course", action = "Details" });
    }

    public int Priority =>
        //it should be the last route
        //we do not set it to -int.MaxValue so it could be overridden (if required)
        -1000000;
}