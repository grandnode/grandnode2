using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Knowledgebase;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Knowledgebase;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Services;

public class KnowledgebaseViewModelService : IKnowledgebaseViewModelService
{
    private readonly IKnowledgebaseService _knowledgebaseService;
    private readonly ISeNameService _seNameService;

    public KnowledgebaseViewModelService(
        IKnowledgebaseService knowledgebaseService,
        ISeNameService seNameService)
    {
        _knowledgebaseService = knowledgebaseService;
        _seNameService = seNameService;
    }

    public virtual async Task PrepareCategory(KnowledgebaseCategoryModel model)
    {
        model.Categories.Add(new SelectListItem { Text = "[None]", Value = "" });
        var categories = await _knowledgebaseService.GetKnowledgebaseCategories();
        foreach (var category in categories.SortCategoriesForTree())
            model.Categories.Add(new SelectListItem {
                Value = category.Id,
                Text = category.GetFormattedBreadCrumb(categories)
            });
    }

    public virtual async Task PrepareCategory(KnowledgebaseArticleModel model)
    {
        model.Categories.Add(new SelectListItem { Text = "[None]", Value = "" });
        var categories = await _knowledgebaseService.GetKnowledgebaseCategories();
        foreach (var category in categories.SortCategoriesForTree())
            model.Categories.Add(new SelectListItem {
                Value = category.Id,
                Text = category.GetFormattedBreadCrumb(categories)
            });
    }

    public virtual async Task<(IEnumerable<KnowledgebaseNodeGridModel> knowledgebaseNodeGridModels, int totalCount)>
        PrepareKnowledgebaseNodeGridModel(string parentCategoryId, int pageIndex, int pageSize)
    {
        var categories = await _knowledgebaseService.GetKnowledgebaseCategories();
        var articles = await _knowledgebaseService.GetKnowledgebaseArticles();

        var parents = TreeParents(categories);
        //an article whose category no longer exists sits at the root, like such a category
        string ArticleParent(KnowledgebaseArticle article)
        {
            return !string.IsNullOrEmpty(article.ParentCategoryId) && parents.ContainsKey(article.ParentCategoryId)
                ? article.ParentCategoryId
                : string.Empty;
        }

        var childCategories = categories.Where(x => parents.ContainsKey(x.Id)).ToLookup(x => parents[x.Id]);
        var childArticles = articles.ToLookup(ArticleParent);
        var parentId = parentCategoryId ?? string.Empty;

        //the children of one category (or of the root): its subcategories, then its articles
        var nodes = childCategories[parentId]
            .OrderBy(x => x.DisplayOrder)
            .Select(category => new KnowledgebaseNodeGridModel {
                Id = category.Id,
                Name = category.Name,
                IsCategory = true,
                ChildCount = childCategories[category.Id].Count() + childArticles[category.Id].Count(),
                Published = category.Published,
                DisplayOrder = category.DisplayOrder
            })
            .Concat(childArticles[parentId]
                .OrderBy(x => x.DisplayOrder)
                .Select(article => new KnowledgebaseNodeGridModel {
                    Id = article.Id,
                    Name = article.Name,
                    IsCategory = false,
                    Published = article.Published,
                    DisplayOrder = article.DisplayOrder
                }))
            .ToList();

        return (nodes.Skip((pageIndex - 1) * pageSize).Take(pageSize), nodes.Count);
    }

    public virtual async
        Task<(IEnumerable<KnowledgebaseArticleGridModel> knowledgebaseArticleGridModels, int totalCount)>
        PrepareKnowledgebaseArticleGridModel(string parentCategoryId, int pageIndex, int pageSize)
    {
        var articles =
            await _knowledgebaseService.GetKnowledgebaseArticlesByCategoryId(parentCategoryId, pageIndex - 1, pageSize);
        return (articles.Select(x => new KnowledgebaseArticleGridModel {
            Name = x.Name,
            DisplayOrder = x.DisplayOrder,
            Published = x.Published,
            ArticleId = x.Id,
            Id = x.Id
        }), articles.TotalCount);
    }

    public virtual async Task<KnowledgebaseCategoryModel> PrepareKnowledgebaseCategoryModel()
    {
        var model = new KnowledgebaseCategoryModel {
            Published = true
        };
        await PrepareCategory(model);
        return model;
    }

    public virtual async Task<KnowledgebaseCategory> InsertKnowledgebaseCategoryModel(KnowledgebaseCategoryModel model)
    {
        var knowledgeBaseCategory = model.ToEntity();
        
        knowledgeBaseCategory.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, knowledgeBaseCategory, x => x.Name);
        knowledgeBaseCategory.SeName = await _seNameService.ValidateSeName(knowledgeBaseCategory, model.SeName, knowledgeBaseCategory.Name, true);

        await _knowledgebaseService.InsertKnowledgebaseCategory(knowledgeBaseCategory);
        await _seNameService.SaveSeName(knowledgeBaseCategory);

        return knowledgeBaseCategory;
    }

    public virtual async Task<KnowledgebaseCategory> UpdateKnowledgebaseCategoryModel(
        KnowledgebaseCategory knowledgebaseCategory, KnowledgebaseCategoryModel model)
    {
        knowledgebaseCategory = model.ToEntity(knowledgebaseCategory);
        knowledgebaseCategory.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, knowledgebaseCategory, x => x.Name);
        knowledgebaseCategory.SeName = await _seNameService.ValidateSeName(knowledgebaseCategory, model.SeName, knowledgebaseCategory.Name, true);

        await _knowledgebaseService.UpdateKnowledgebaseCategory(knowledgebaseCategory);
        await _seNameService.SaveSeName(knowledgebaseCategory);

        return knowledgebaseCategory;
    }

    public virtual async Task DeleteKnowledgebaseCategoryModel(KnowledgebaseCategory knowledgebaseCategory)
    {
        await _knowledgebaseService.DeleteKnowledgebaseCategory(knowledgebaseCategory);
    }

    public virtual async Task<KnowledgebaseArticleModel> PrepareKnowledgebaseArticleModel()
    {
        var model = new KnowledgebaseArticleModel {
            Published = true,
            AllowComments = true
        };
        await PrepareCategory(model);
        return model;
    }

    public virtual async Task<KnowledgebaseArticle> InsertKnowledgebaseArticleModel(KnowledgebaseArticleModel model)
    {
        var knowledgeBaseArticle = model.ToEntity();
        knowledgeBaseArticle.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, knowledgeBaseArticle, x => x.Name);
        knowledgeBaseArticle.SeName = await _seNameService.ValidateSeName(knowledgeBaseArticle, model.SeName, knowledgeBaseArticle.Name, true);

        knowledgeBaseArticle.AllowComments = model.AllowComments;
        
        await _knowledgebaseService.InsertKnowledgebaseArticle(knowledgeBaseArticle);
        await _seNameService.SaveSeName(knowledgeBaseArticle);
        
        return knowledgeBaseArticle;
    }

    public virtual async Task<KnowledgebaseArticle> UpdateKnowledgebaseArticleModel(
        KnowledgebaseArticle knowledgeBaseArticle, KnowledgebaseArticleModel model)
    {
        knowledgeBaseArticle = model.ToEntity(knowledgeBaseArticle);
        knowledgeBaseArticle.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, knowledgeBaseArticle, x => x.Name);
        knowledgeBaseArticle.SeName = await _seNameService.ValidateSeName(knowledgeBaseArticle, model.SeName, knowledgeBaseArticle.Name, true);
        
        knowledgeBaseArticle.AllowComments = model.AllowComments;

        await _knowledgebaseService.UpdateKnowledgebaseArticle(knowledgeBaseArticle);
        await _seNameService.SaveSeName(knowledgeBaseArticle);

        return knowledgeBaseArticle;
    }

    public virtual async Task DeleteKnowledgebaseArticle(KnowledgebaseArticle knowledgebaseArticle)
    {
        await _knowledgebaseService.DeleteKnowledgebaseArticle(knowledgebaseArticle);
    }

    public virtual async Task InsertKnowledgebaseRelatedArticle(KnowledgebaseArticleModel.AddRelatedArticleModel model)
    {
        var article = await _knowledgebaseService.GetKnowledgebaseArticle(model.ArticleId);

        foreach (var id in model.SelectedArticlesIds)
            if (id != article.Id)
                if (!article.RelatedArticles.Contains(id))
                    article.RelatedArticles.Add(id);
        await _knowledgebaseService.UpdateKnowledgebaseArticle(article);
    }

    public virtual async Task DeleteKnowledgebaseRelatedArticle(KnowledgebaseArticleModel.AddRelatedArticleModel model)
    {
        var article = await _knowledgebaseService.GetKnowledgebaseArticle(model.ArticleId);
        var related = await _knowledgebaseService.GetKnowledgebaseArticle(model.Id);
        ArgumentNullException.ThrowIfNull(article);
        ArgumentNullException.ThrowIfNull(related);

        var toDelete = "";
        foreach (var item in article.RelatedArticles)
            if (item == related.Id)
                toDelete = item;

        if (!string.IsNullOrEmpty(toDelete))
            article.RelatedArticles.Remove(toDelete);

        await _knowledgebaseService.UpdateKnowledgebaseArticle(article);
    }

    /// <summary>
    ///     The category each category is listed under, by id; empty for the root. A category
    ///     with no parent id (null or empty) or with a parent that no longer exists is a root.
    ///     Categories caught in a parent cycle have no root, so the first category of the
    ///     cycle stands in for one: every category is listed once, and expanding a row always ends.
    /// </summary>
    protected static Dictionary<string, string> TreeParents(IList<KnowledgebaseCategory> categories)
    {
        var byId = categories.DistinctBy(x => x.Id).ToDictionary(x => x.Id);
        var children = categories
            .Where(x => !string.IsNullOrEmpty(x.ParentCategoryId) && byId.ContainsKey(x.ParentCategoryId))
            .ToLookup(x => x.ParentCategoryId);
        var parents = new Dictionary<string, string>();

        void Add(KnowledgebaseCategory category, string parentId)
        {
            //guards against a circular parent chain
            if (!parents.TryAdd(category.Id, parentId)) return;
            foreach (var child in children[category.Id].OrderBy(x => x.DisplayOrder))
                Add(child, category.Id);
        }

        foreach (var root in categories
                     .Where(x => string.IsNullOrEmpty(x.ParentCategoryId) || !byId.ContainsKey(x.ParentCategoryId))
                     .OrderBy(x => x.DisplayOrder))
            Add(root, string.Empty);

        foreach (var category in categories.Where(x => !parents.ContainsKey(x.Id)).ToList())
        {
            if (parents.ContainsKey(category.Id)) continue;
            //climb to the cycle above the category, so a category hanging under a cycle stays under it
            var top = category;
            var seen = new HashSet<string>();
            while (seen.Add(top.Id) && !string.IsNullOrEmpty(top.ParentCategoryId) &&
                   byId.TryGetValue(top.ParentCategoryId, out var parent))
                top = parent;
            Add(top, string.Empty);
        }

        return parents;
    }
}