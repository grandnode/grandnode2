using Grand.Domain.Pages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Layouts;

namespace Grand.Web.Admin.Extensions.Mapping.Layouts;

public static class PageLayoutMappingExtensions
{
    public static PageLayoutModel ToModel(this PageLayout entity)
    {
        return entity.MapTo<PageLayout, PageLayoutModel>();
    }

    public static PageLayout ToEntity(this PageLayoutModel model)
    {
        return model.MapTo<PageLayoutModel, PageLayout>();
    }

    public static PageLayout ToEntity(this PageLayoutModel model, PageLayout destination)
    {
        return model.MapTo(destination);
    }
}