using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class ProductReviewMappingExtensions
{
    public static ProductReview ToEntity(this ProductReviewModel model, ProductReview destination)
    {
        return model.MapTo(destination);
    }
}