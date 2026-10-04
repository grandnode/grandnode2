using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Tax;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class ITaxProviderMappingExtensions
{
    public static TaxProviderModel ToModel(this ITaxProvider entity)
    {
        return entity.MapTo<ITaxProvider, TaxProviderModel>();
    }
}