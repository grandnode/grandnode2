using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class CategoryMappingExtensions
{
    public static CategoryModel ToModel(this Category entity)
    {
        return entity.MapTo<Category, CategoryModel>();
    }

    public static Category ToEntity(this CategoryModel model)
    {
        return model.MapTo<CategoryModel, Category>();
    }

    public static Category ToEntity(this CategoryModel model, Category destination)
    {
        return model.MapTo(destination);
    }
}