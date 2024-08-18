using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Knowledgebase;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Knowledgebase;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

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

    public virtual async Task<List<TreeNode>> PrepareTreeNode()
    {
        var categories = await _knowledgebaseService.GetKnowledgebaseCategories();
        var articles = await _knowledgebaseService.GetKnowledgebaseArticles();
        var nodeList = new List<TreeNode>();

        var list = new List<ITreeNode>();
        list.AddRange(categories.SortCategoriesForTree());
        list.AddRange(articles);

        foreach (var node in list)
            if (string.IsNullOrEmpty(node.ParentCategoryId))
            {
                var newNode = new TreeNode {
                    id = node.Id,
                    text = node.Name,
                    isCategory = node.GetType() == typeof(KnowledgebaseCategory),
                    nodes = new List<TreeNode>()
                };

                FillChildNodes(newNode, list);

                nodeList.Add(newNode);
            }

        return nodeList;
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

        if (article == null || related == null)
            throw new ArgumentNullException("No article found with specified id");

        var toDelete = "";
        foreach (var item in article.RelatedArticles)
            if (item == related.Id)
                toDelete = item;

        if (!string.IsNullOrEmpty(toDelete))
            article.RelatedArticles.Remove(toDelete);

        await _knowledgebaseService.UpdateKnowledgebaseArticle(article);
    }

    protected virtual void FillChildNodes(TreeNode parentNode, IEnumerable<ITreeNode> nodes)
    {
        var treeNodes = nodes.ToList();
        var children = treeNodes.Where(x => x.ParentCategoryId == parentNode.id);
        foreach (var child in children)
        {
            var newNode = new TreeNode {
                id = child.Id,
                text = child.Name,
                isCategory = child.GetType() == typeof(KnowledgebaseCategory),
                nodes = new List<TreeNode>()
            };

            FillChildNodes(newNode, treeNodes);

            parentNode.nodes.Add(newNode);
        }
    }
}