using Grand.Business.Core.Extensions;
using Grand.Domain.Knowledgebase;

namespace Grand.Web.Extensions;

public static class KnowledgebaseExtensions
{
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