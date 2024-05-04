using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Services.ExportImport;

public class CollectionSchemaProperty : ISchemaProperty<Collection>
{
    private readonly IPictureService _pictureService;

    public CollectionSchemaProperty(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    public virtual async Task<PropertyByName<Collection>[]> GetProperties()
    {
        var properties = new[] {
            new PropertyByName<Collection>("Id", p => p.Id),
            new PropertyByName<Collection>("Name", p => p.Name),
            new PropertyByName<Collection>("Description", p => p.Description),
            new PropertyByName<Collection>("CollectionLayoutId", p => p.CollectionLayoutId),
            new PropertyByName<Collection>("MetaKeywords", p => p.MetaKeywords),
            new PropertyByName<Collection>("MetaDescription", p => p.MetaDescription),
            new PropertyByName<Collection>("MetaTitle", p => p.MetaTitle),
            new PropertyByName<Collection>("SeName", p => p.SeName),
            new PropertyByName<Collection>("Picture", p => GetPictures(p.PictureId).Result),
            new PropertyByName<Collection>("PageSize", p => p.PageSize),
            new PropertyByName<Collection>("AllowCustomersToSelectPageSize", p => p.AllowCustomersToSelectPageSize),
            new PropertyByName<Collection>("PageSizeOptions", p => p.PageSizeOptions),
            new PropertyByName<Collection>("Published", p => p.Published),
            new PropertyByName<Collection>("DisplayOrder", p => p.DisplayOrder),
            new PropertyByName<Collection>("ExternalId", p => p.ExternalId)
        };
        return await Task.FromResult(properties);
    }

    protected virtual async Task<string> GetPictures(string pictureId)
    {
        var picture = await _pictureService.GetPictureById(pictureId);
        return await _pictureService.GetThumbPhysicalPath(picture);
    }
}