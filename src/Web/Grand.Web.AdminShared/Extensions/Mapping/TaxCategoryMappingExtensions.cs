using Grand.Domain.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Tax;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class TaxCategoryMappingExtensions
{
    public static TaxCategoryModel ToModel(this TaxCategory entity)
    {
        return entity.MapTo<TaxCategory, TaxCategoryModel>();
    }

    public static TaxCategory ToEntity(this TaxCategoryModel model)
    {
        return model.MapTo<TaxCategoryModel, TaxCategory>();
    }

    public static TaxCategory ToEntity(this TaxCategoryModel model, TaxCategory destination)
    {
        return model.MapTo(destination);
    }
}