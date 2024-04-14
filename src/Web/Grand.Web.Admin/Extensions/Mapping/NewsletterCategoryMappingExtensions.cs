using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Extensions.Mapping;

public static class NewsletterCategoryMappingExtensions
{
    public static NewsletterCategoryModel ToModel(this NewsletterCategory entity)
    {
        return entity.MapTo<NewsletterCategory, NewsletterCategoryModel>();
    }

    public static NewsletterCategory ToEntity(this NewsletterCategoryModel model)
    {
        return model.MapTo<NewsletterCategoryModel, NewsletterCategory>();
    }

    public static NewsletterCategory ToEntity(this NewsletterCategoryModel model, NewsletterCategory destination)
    {
        return model.MapTo(destination);
    }
}