using Grand.Infrastructure.Mapper;
using Grand.Domain.Customers;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Extensions
{
    public static class CustomerGroupMappingExtensions
    {
        public static CustomerGroupModel ToModel(this CustomerGroup entity)
        {
            return entity.MapTo<CustomerGroup, CustomerGroupModel>();
        }

        public static CustomerGroup ToEntity(this CustomerGroupModel model)
        {
            return model.MapTo<CustomerGroupModel, CustomerGroup>();
        }

        public static CustomerGroup ToEntity(this CustomerGroupModel model, CustomerGroup destination)
        {
            return model.MapTo(destination);
        }
    }
}