using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Models.Knowledgebase;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class KnowledgebaseCategories : BaseViewComponent
{
    private readonly IKnowledgebaseService _knowledgebaseService;
    private readonly KnowledgebaseSettings _knowledgebaseSettings;
    private readonly IWorkContext _workContext;

    public KnowledgebaseCategories(IKnowledgebaseService knowledgebaseService, IWorkContext workContext,
        KnowledgebaseSettings knowledgebaseSettings)
    {
        _knowledgebaseService = knowledgebaseService;
        _workContext = workContext;
        _knowledgebaseSettings = knowledgebaseSettings;
    }

    public async Task<IViewComponentResult> InvokeAsync(KnowledgebaseHomePageModel model)
    {
        if (!_knowledgebaseSettings.Enabled)
            return Content("");

        var categories = await _knowledgebaseService.GetPublicKnowledgebaseCategories();

        foreach (var category in categories)
        {
            if (!string.IsNullOrEmpty(category.ParentCategoryId)) continue;
            var newNode = new KnowledgebaseCategoryModel {
                Id = category.Id,
                Name = category.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id),
                Children = new List<KnowledgebaseCategoryModel>(),
                IsCurrent = model.CurrentCategoryId == category.Id,
                SeName = category.GetTranslation(y => y.SeName, _workContext.WorkingLanguage.Id)
            };

            FillChildNodes(newNode, categories, model.CurrentCategoryId);

            model.Categories.Add(newNode);
        }

        return View(model);
    }

    public void FillChildNodes(KnowledgebaseCategoryModel parentNode, List<KnowledgebaseCategory> nodes,
        string currentCategoryId)
    {
        var children = nodes.Where(x => x.ParentCategoryId == parentNode.Id);
        foreach (var child in children)
        {
            var newNode = new KnowledgebaseCategoryModel {
                Id = child.Id,
                Name = child.GetTranslation(y => y.Name, _workContext.WorkingLanguage.Id),
                Children = new List<KnowledgebaseCategoryModel>(),
                IsCurrent = currentCategoryId == child.Id,
                SeName = child.GetTranslation(y => y.SeName, _workContext.WorkingLanguage.Id),
                Parent = parentNode
            };

            if (newNode.IsCurrent) MarkParentsAsCurrent(newNode);

            FillChildNodes(newNode, nodes, currentCategoryId);

            parentNode.Children.Add(newNode);
        }
    }

    public void MarkParentsAsCurrent(KnowledgebaseCategoryModel node)
    {
        if (node.Parent == null) return;
        node.Parent.IsCurrent = true;
        MarkParentsAsCurrent(node.Parent);
    }
}