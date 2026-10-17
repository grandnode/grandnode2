using FluentValidation;
using Grand.Module.Api.Extensions;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;

namespace Grand.Module.Api.Validators.Catalog;

public class CategoryValidator : BaseGrandValidator<CategoryDto>
{
    public CategoryValidator(IEnumerable<IValidatorConsumer<CategoryDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor, IPictureService pictureService, ICategoryService categoryService,
        ICategoryLayoutService
            categoryLayoutService) : base(validators)
    {
        var languageId = contextAccessor.WorkingLanguageId();

        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Api.Catalog.Category.Fields.Name.Required", languageId));
        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.PictureId))
            {
                var picture = await pictureService.GetPictureById(x.PictureId);
                if (picture == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Category.Fields.PictureId.NotExists", languageId));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.ParentCategoryId))
            {
                var category = await categoryService.GetCategoryById(x.ParentCategoryId);
                if (category == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Category.Fields.ParentCategoryId.NotExists", languageId));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.CategoryLayoutId))
            {
                var layout = await categoryLayoutService.GetCategoryLayoutById(x.CategoryLayoutId);
                if (layout == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Category.Fields.CategoryLayoutId.NotExists", languageId));

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.Id))
            {
                var category = await categoryService.GetCategoryById(x.Id);
                if (category == null)
                    return false;
            }

            return true;
        }).WithMessage(translationService.GetResource("Api.Catalog.Category.Fields.Id.NotExists", languageId));
    }
}