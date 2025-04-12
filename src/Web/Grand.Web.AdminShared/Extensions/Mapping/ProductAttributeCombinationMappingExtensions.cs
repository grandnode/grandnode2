using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Catalog;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class ProductAttributeCombinationMappingExtensions
{
    public static ProductAttributeCombinationModel ToModel(this ProductAttributeCombination entity)
    {
        return entity.MapTo<ProductAttributeCombination, ProductAttributeCombinationModel>();
    }

    public static ProductAttributeCombination ToEntity(this ProductAttributeCombinationModel model)
    {
        return model.MapTo<ProductAttributeCombinationModel, ProductAttributeCombination>();
    }

    public static ProductAttributeCombination ToEntity(this ProductAttributeCombinationModel model,
        ProductAttributeCombination destination)
    {
        return model.MapTo(destination);
    }
}