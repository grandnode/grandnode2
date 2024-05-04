using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Services.ExportImport;

public class CategorySchemaProperty : ISchemaProperty<Category>
{
    private readonly IPictureService _pictureService;

    public CategorySchemaProperty(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    public virtual async Task<PropertyByName<Category>[]> GetProperties()
    {
        var properties = new[] {
            new PropertyByName<Category>("Id", p => p.Id),
            new PropertyByName<Category>("Name", p => p.Name),
            new PropertyByName<Category>("Description", p => p.Description),
            new PropertyByName<Category>("CategoryLayoutId", p => p.CategoryLayoutId),
            new PropertyByName<Category>("MetaKeywords", p => p.MetaKeywords),
            new PropertyByName<Category>("MetaDescription", p => p.MetaDescription),
            new PropertyByName<Category>("MetaTitle", p => p.MetaTitle),
            new PropertyByName<Category>("SeName", p => p.SeName),
            new PropertyByName<Category>("ParentCategoryId", p => p.ParentCategoryId),
            new PropertyByName<Category>("Picture", p => GetPictures(p.PictureId).Result),
            new PropertyByName<Category>("PageSize", p => p.PageSize),
            new PropertyByName<Category>("AllowCustomersToSelectPageSize", p => p.AllowCustomersToSelectPageSize),
            new PropertyByName<Category>("PageSizeOptions", p => p.PageSizeOptions),
            new PropertyByName<Category>("ShowOnHomePage", p => p.ShowOnHomePage),
            new PropertyByName<Category>("IncludeInMenu", p => p.IncludeInMenu),
            new PropertyByName<Category>("Published", p => p.Published),
            new PropertyByName<Category>("ExternalId", p => p.ExternalId),
            new PropertyByName<Category>("Flag", p => p.Flag),
            new PropertyByName<Category>("FlagStyle", p => p.FlagStyle),
            new PropertyByName<Category>("Icon", p => p.Icon),
            new PropertyByName<Category>("DisplayOrder", p => p.DisplayOrder)
        };
        return await Task.FromResult(properties);
    }

    protected virtual async Task<string> GetPictures(string pictureId)
    {
        var picture = await _pictureService.GetPictureById(pictureId);
        return await _pictureService.GetThumbPhysicalPath(picture);
    }
}