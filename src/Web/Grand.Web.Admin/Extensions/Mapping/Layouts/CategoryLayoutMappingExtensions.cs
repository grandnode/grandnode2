using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Layouts;

namespace Grand.Web.Admin.Extensions.Mapping.Layouts;

public static class CategoryLayoutMappingExtensions
{
    public static CategoryLayoutModel ToModel(this CategoryLayout entity)
    {
        return entity.MapTo<CategoryLayout, CategoryLayoutModel>();
    }

    public static CategoryLayout ToEntity(this CategoryLayoutModel model)
    {
        return model.MapTo<CategoryLayoutModel, CategoryLayout>();
    }

    public static CategoryLayout ToEntity(this CategoryLayoutModel model, CategoryLayout destination)
    {
        return model.MapTo(destination);
    }
}