using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Services.ExportImport;

public class BrandSchemaProperty : ISchemaProperty<Brand>
{
    private readonly IPictureService _pictureService;

    public BrandSchemaProperty(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    public virtual async Task<PropertyByName<Brand>[]> GetProperties()
    {
        var properties = new[] {
            new PropertyByName<Brand>("Id", p => p.Id),
            new PropertyByName<Brand>("Name", p => p.Name),
            new PropertyByName<Brand>("Description", p => p.Description),
            new PropertyByName<Brand>("BrandLayoutId", p => p.BrandLayoutId),
            new PropertyByName<Brand>("MetaKeywords", p => p.MetaKeywords),
            new PropertyByName<Brand>("MetaDescription", p => p.MetaDescription),
            new PropertyByName<Brand>("MetaTitle", p => p.MetaTitle),
            new PropertyByName<Brand>("SeName", p => p.SeName),
            new PropertyByName<Brand>("Picture", p => GetPictures(p.PictureId).Result),
            new PropertyByName<Brand>("PageSize", p => p.PageSize),
            new PropertyByName<Brand>("AllowCustomersToSelectPageSize", p => p.AllowCustomersToSelectPageSize),
            new PropertyByName<Brand>("PageSizeOptions", p => p.PageSizeOptions),
            new PropertyByName<Brand>("Published", p => p.Published),
            new PropertyByName<Brand>("DisplayOrder", p => p.DisplayOrder),
            new PropertyByName<Brand>("ExternalId", p => p.ExternalId)
        };
        return await Task.FromResult(properties);
    }

    protected virtual async Task<string> GetPictures(string pictureId)
    {
        var picture = await _pictureService.GetPictureById(pictureId);
        return await _pictureService.GetThumbPhysicalPath(picture);
    }
}