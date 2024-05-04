using Grand.Api.Commands.Models.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly ICategoryService _categoryService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public DeleteCategoryCommandHandler(
        ICategoryService categoryService,
        ITranslationService translationService,
        IWorkContext workContext)
    {
        _categoryService = categoryService;
        _translationService = translationService;
        _workContext = workContext;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetCategoryById(request.Model.Id);
        if (category != null) await _categoryService.DeleteCategory(category);
        return true;
    }
}