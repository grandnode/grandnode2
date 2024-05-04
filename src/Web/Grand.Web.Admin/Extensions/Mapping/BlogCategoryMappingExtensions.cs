using Grand.Domain.Blogs;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Blogs;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class BlogCategoryMappingExtensions
{
    public static BlogCategoryModel ToModel(this BlogCategory entity)
    {
        return entity.MapTo<BlogCategory, BlogCategoryModel>();
    }

    public static BlogCategory ToEntity(this BlogCategoryModel model)
    {
        return model.MapTo<BlogCategoryModel, BlogCategory>();
    }

    public static BlogCategory ToEntity(this BlogCategoryModel model, BlogCategory destination)
    {
        return model.MapTo(destination);
    }
}