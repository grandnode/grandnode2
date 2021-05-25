using Grand.Domain.Data;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Endpoints;
using Grand.Web.Common.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Endpoints
{
    public partial class SlugEndpointProvider : IEndpointProvider
    {
        public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            var pattern = "{**SeName}";
            var config = endpointRouteBuilder.ServiceProvider.GetRequiredService<AppConfig>();
            if (config.SeoFriendlyUrlsForLanguagesEnabled)
            {
                pattern = $"{{language:lang={config.SeoFriendlyUrlsDefaultCode}}}/{{**SeName}}";
            }

            endpointRouteBuilder.MapDynamicControllerRoute<SlugRouteTransformer>(pattern);

            //and default one
            endpointRouteBuilder.MapControllerRoute(
                name: "Default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            //generic URLs
            endpointRouteBuilder.MapControllerRoute(
                name: "GenericUrl",
                pattern: "{GenericSeName}",
                new { controller = "Common", action = "GenericUrl" });

            //define this routes to use in UI views (in case if you want to customize some of them later)
            endpointRouteBuilder.MapControllerRoute(
                name: "Product",
                pattern: pattern,
                new { controller = "Product", action = "ProductDetails" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Category",
                pattern: pattern,
                new { controller = "Catalog", action = "Category" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Brand",
                pattern: pattern,
                new { controller = "Catalog", action = "Brand" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Collection",
                pattern: pattern,
                new { controller = "Catalog", action = "Collection" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Vendor",
                pattern: pattern,
                new { controller = "Catalog", action = "Vendor" });

            endpointRouteBuilder.MapControllerRoute(
                name: "NewsItem",
                pattern: pattern,
                new { controller = "News", action = "NewsItem" });

            endpointRouteBuilder.MapControllerRoute(
                name: "BlogPost",
                pattern: pattern,
                new { controller = "Blog", action = "BlogPost" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Page",
                pattern: pattern,
                new { controller = "Page", action = "PageDetails" });

            endpointRouteBuilder.MapControllerRoute(
                name: "KnowledgebaseArticle",
                pattern: pattern,
                new { controller = "Knowledgebase", action = "KnowledgebaseArticle" });

            endpointRouteBuilder.MapControllerRoute(
                name: "KnowledgebaseCategory",
                pattern: pattern,
                new { controller = "Knowledgebase", action = "ArticlesByCategory" });

            endpointRouteBuilder.MapControllerRoute(
                name: "Course",
                pattern: pattern,
                new { controller = "Course", action = "Details" });

        }

        public int Priority {
            get {
                //it should be the last route
                //we do not set it to -int.MaxValue so it could be overridden (if required)
                return -1000000;
            }
        }
    }
}
