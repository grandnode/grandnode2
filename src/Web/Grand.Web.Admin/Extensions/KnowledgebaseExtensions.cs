using Grand.Business.Core.Extensions;
using Grand.Domain.Knowledgebase;

namespace Grand.Web.Admin.Extensions;

/// <summary>
///     Extensions
/// </summary>
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

    /// <summary>
    ///     Get category breadcrumb
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="allCategories">All categories</param>
    /// <returns>Category breadcrumb </returns>
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

    public static List<KnowledgebaseCategory> SortCategoriesForTree(this IList<KnowledgebaseCategory> source,
        string parentId = null,
        bool ignoreCategoriesWithoutExistingParent = false)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = new List<KnowledgebaseCategory>();

        foreach (var cat in source.Where(c => c.ParentCategoryId == parentId).ToList())
        {
            result.Add(cat);
            result.AddRange(SortCategoriesForTree(source, cat.Id, true));
        }

        if (!ignoreCategoriesWithoutExistingParent && result.Count != source.Count)
            //find categories without parent in provided category source and insert them into result
            foreach (var cat in source)
                if (result.FirstOrDefault(x => x.Id == cat.Id) == null)
                    result.Add(cat);
        return result;
    }
}