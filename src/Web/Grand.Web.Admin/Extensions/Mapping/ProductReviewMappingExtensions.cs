using Grand.Infrastructure.Mapper;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Extensions
{
    public static class ProductReviewMappingExtensions
    {
        public static ProductReview ToEntity(this ProductReviewModel model, ProductReview destination)
        {
            return model.MapTo(destination);
        }
    }
}