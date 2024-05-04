using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Seo;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Common.Routing;

public class SlugRouteTransformer : DynamicRouteValueTransformer
{
    private readonly AppConfig _config;
    private readonly ILanguageService _languageService;
    private readonly ISlugService _slugService;

    public SlugRouteTransformer(
        ISlugService slugService,
        ILanguageService languageService,
        AppConfig config)
    {
        _slugService = slugService;
        _languageService = languageService;
        _config = config;
    }

    private async ValueTask<string> GetSeName(string entityId, string entityName, string languageId)
    {
        var result = string.Empty;
        if (!string.IsNullOrEmpty(languageId))
            result = await _slugService.GetActiveSlug(entityId, entityName, languageId);
        //set default value if required
        if (string.IsNullOrEmpty(result)) result = await _slugService.GetActiveSlug(entityId, entityName, "");

        return result;
    }

    public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext context,
        RouteValueDictionary values)
    {
        var slug = values["SeName"];
        if (slug == null)
            return values;

        var entityUrl = await _slugService.GetBySlugCached(slug.ToString());

        //no URL Entity found
        if (entityUrl == null)
            return null;

        //if URL Entity is not active find the latest one
        if (!entityUrl.IsActive)
        {
            var activeSlug =
                await _slugService.GetActiveSlug(entityUrl.EntityId, entityUrl.EntityName, entityUrl.LanguageId);
            if (string.IsNullOrEmpty(activeSlug))
                return null;

            values["controller"] = "Common";
            values["action"] = "InternalRedirect";
            values["url"] = $"{context.Request.PathBase}/{activeSlug}{context.Request.QueryString}";
            values["permanentRedirect"] = true;
            context.Items["grand.RedirectFromGenericPathRoute"] = true;
            return values;
        }

        //ensure that the slug is the same for the current language, 
        //otherwise it can cause some issues when customers choose a new language but a slug stays the same
        if (_config.SeoFriendlyUrlsForLanguagesEnabled)
        {
            var urlLanguage = values["language"];
            if (urlLanguage != null && !string.IsNullOrEmpty(urlLanguage.ToString()))
            {
                var language = (await _languageService.GetAllLanguages()).FirstOrDefault(x =>
                                   x.UniqueSeoCode.ToLowerInvariant() == urlLanguage.ToString()?.ToLowerInvariant()) ??
                               (await _languageService.GetAllLanguages()).FirstOrDefault();

                var slugForCurrentLanguage = await GetSeName(entityUrl.EntityId, entityUrl.EntityName, language?.Id);
                if (!string.IsNullOrEmpty(slugForCurrentLanguage) &&
                    !slugForCurrentLanguage.Equals(slug.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    values["controller"] = "Common";
                    values["action"] = "InternalRedirect";
                    values["url"] = $"{context.Request.PathBase}/{slugForCurrentLanguage}{context.Request.QueryString}";
                    values["permanentRedirect"] = false;
                    context.Items["grand.RedirectFromGenericPathRoute"] = true;
                    return values;
                }
            }
        }

        switch (entityUrl.EntityName)
        {
            case EntityTypes.Product:
                values["controller"] = "Product";
                values["action"] = "ProductDetails";
                values["productid"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.Category:
                values["controller"] = "Catalog";
                values["action"] = "Category";
                values["categoryid"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.Brand:
                values["controller"] = "Catalog";
                values["action"] = "Brand";
                values["brandid"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.Collection:
                values["controller"] = "Catalog";
                values["action"] = "Collection";
                values["collectionid"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.Vendor:
                values["controller"] = "Catalog";
                values["action"] = "Vendor";
                values["vendorid"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.NewsItem:
                values["controller"] = "News";
                values["action"] = "NewsItem";
                values["newsItemId"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.BlogPost:
                values["controller"] = "Blog";
                values["action"] = "BlogPost";
                values["blogPostId"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.Page:
                values["controller"] = "Page";
                values["action"] = "PageDetails";
                values["pageId"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.KnowledgeBaseArticle:
                values["controller"] = "Knowledgebase";
                values["action"] = "KnowledgebaseArticle";
                values["articleId"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.KnowledgeBaseCategory:
                values["controller"] = "Knowledgebase";
                values["action"] = "ArticlesByCategory";
                values["categoryId"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            case EntityTypes.Course:
                values["controller"] = "Course";
                values["action"] = "Details";
                values["courseId"] = entityUrl.EntityId;
                values["SeName"] = entityUrl.Slug;
                break;
            default:
                if (!string.IsNullOrEmpty(entityUrl.Controller) && !string.IsNullOrEmpty(entityUrl.Action))
                {
                    values["controller"] = entityUrl.Controller;
                    values["action"] = entityUrl.Action;
                    values["SeName"] = entityUrl.Slug;
                }

                break;
        }

        return values;
    }
}