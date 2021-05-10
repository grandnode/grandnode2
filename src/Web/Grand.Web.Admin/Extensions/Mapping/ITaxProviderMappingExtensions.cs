using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Tax;

namespace Grand.Web.Admin.Extensions
{
    public static class ITaxProviderMappingExtensions
    {
        public static TaxProviderModel ToModel(this ITaxProvider entity)
        {
            return entity.MapTo<ITaxProvider, TaxProviderModel>();
        }
    }
}