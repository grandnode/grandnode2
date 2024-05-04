using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure;

namespace Grand.Web.Extensions;

public static class KnowledgebaseExtensions
{
    /// <summary>
    ///     Get category breadcrumb
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="knowledgebaseService">Knowledge base Service</param>
    /// <param name="aclService">ACL service</param>
    /// <param name="workContext">WorkContext</param>
    /// <param name="showHidden">A value indicating whether to load hidden records</param>
    /// <returns>Category breadcrumb </returns>
    public static async Task<IList<KnowledgebaseCategory>> GetCategoryBreadCrumb(this KnowledgebaseCategory category,
        IKnowledgebaseService knowledgebaseService,
        IAclService aclService,
        IWorkContext workContext,
        bool showHidden = false)
    {
        ArgumentNullException.ThrowIfNull(category);

        var result = new List<KnowledgebaseCategory>();

        //used to prevent circular references
        var alreadyProcessedCategoryIds = new List<string>();

        while (category != null && //not null                
               (showHidden || category.Published) && //published
               (showHidden || aclService.Authorize(category, workContext.CurrentCustomer)) && //ACL
               (showHidden || aclService.Authorize(category, workContext.CurrentStore.Id)) && //Store acl
               !alreadyProcessedCategoryIds.Contains(category.Id)) //prevent circular references
        {
            result.Add(category);

            alreadyProcessedCategoryIds.Add(category.Id);

            category = await knowledgebaseService.GetKnowledgebaseCategory(category.ParentCategoryId);
        }

        result.Reverse();
        return result;
    }

    /// <summary>
    ///     Get formatted category breadcrumb
    ///     Note: ACL and store acl is ignored
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="allCategories">All categories</param>
    /// <param name="separator">Separator</param>
    /// <param name="languageId">Language identifier for localization</param>
    /// <returns>Formatted breadcrumb</returns>
    public static string GetFormattedBreadCrumb(this KnowledgebaseCategory category,
        IList<KnowledgebaseCategory> allCategories,
        string separator = ">>", string languageId = "")
    {
        var result = string.Empty;

        var breadcrumb = GetCategoryBreadCrumb(category, allCategories);
        for (var i = 0; i <= breadcrumb.Count - 1; i++)
        {
            var categoryName = breadcrumb[i].GetTranslation(x => x.Name, languageId);
            result = string.IsNullOrEmpty(result)
                ? categoryName
                : $"{result} {separator} {categoryName}";
        }

        return result;
    }

    public static IList<KnowledgebaseCategory> GetCategoryBreadCrumb(this KnowledgebaseCategory category,
        IList<KnowledgebaseCategory> allCategories)
    {
        ArgumentNullException.ThrowIfNull(category);

        var result = new List<KnowledgebaseCategory>();

        //used to prevent circular references
        var alreadyProcessedCategoryIds = new List<string>();

        while (category != null && !alreadyProcessedCategoryIds.Contains(category.Id)) //prevent circular references
        {
            result.Add(category);

            alreadyProcessedCategoryIds.Add(category.Id);

            category = (from c in allCategories
                where c.Id == category.ParentCategoryId
                select c).FirstOrDefault();
        }

        result.Reverse();
        return result;
    }
}