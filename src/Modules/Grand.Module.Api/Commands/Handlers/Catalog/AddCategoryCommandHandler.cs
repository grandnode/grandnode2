using Grand.Module.Api.Commands.Models.Catalog;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Catalog;

public class AddCategoryCommandHandler : IRequestHandler<AddCategoryCommand, CategoryDto>
{
    private readonly ICategoryService _categoryService;
    private readonly ISlugService _slugService;
    private readonly ISeNameService _seNameService;

    public AddCategoryCommandHandler(
        ICategoryService categoryService,
        ISlugService slugService,
        ISeNameService seNameService)
    {
        _categoryService = categoryService;
        _slugService = slugService;
        _seNameService = seNameService;
    }

    public async Task<CategoryDto> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = request.Model.ToEntity();
        await _categoryService.InsertCategory(category);
        request.Model.SeName = await _seNameService.ValidateSeName(category, request.Model.SeName, category.Name, true);
        category.SeName = request.Model.SeName;
        await _categoryService.UpdateCategory(category);
        await _slugService.SaveSlug(category, request.Model.SeName, "");

        return category.ToModel();
    }
}