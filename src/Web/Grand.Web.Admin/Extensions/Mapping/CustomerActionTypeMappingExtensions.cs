using Grand.Infrastructure.Mapper;
using Grand.Domain.Customers;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Extensions
{
    public static class CustomerActionTypeMappingExtensions
    {
        public static CustomerActionTypeModel ToModel(this CustomerActionType entity)
        {
            return entity.MapTo<CustomerActionType, CustomerActionTypeModel>();
        }
    }
}