using Grand.Api.Commands.Models.Catalog;
using Grand.Api.DTOs.Catalog;
using Grand.Api.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using MediatR;

namespace Grand.Api.Commands.Handlers.Catalog;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryService _categoryService;
    private readonly IPictureService _pictureService;
    private readonly ISlugService _slugService;
    private readonly ISlugNameValidator _slugNameValidator;
    public UpdateCategoryCommandHandler(
        ICategoryService categoryService,
        ISlugService slugService,
        IPictureService pictureService,
        ISlugNameValidator slugNameValidator)
    {
        _categoryService = categoryService;
        _slugService = slugService;
        _pictureService = pictureService;
        _slugNameValidator = slugNameValidator;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetCategoryById(request.Model.Id);
        var prevPictureId = category.PictureId;
        category = request.Model.ToEntity(category);
        request.Model.SeName = await _slugNameValidator.ValidateSeName(category, request.Model.SeName, category.Name, true);
        category.SeName = request.Model.SeName;
        await _categoryService.UpdateCategory(category);
        //search engine name
        await _slugService.SaveSlug(category, request.Model.SeName, "");
        await _categoryService.UpdateCategory(category);
        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != category.PictureId)
        {
            var prevPicture = await _pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await _pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        if (!string.IsNullOrEmpty(category.PictureId))
        {
            var picture = await _pictureService.GetPictureById(category.PictureId);
            if (picture != null)
                await _pictureService.SetSeoFilename(picture, _pictureService.GetPictureSeName(category.Name));
        }

        return category.ToModel();
    }
}