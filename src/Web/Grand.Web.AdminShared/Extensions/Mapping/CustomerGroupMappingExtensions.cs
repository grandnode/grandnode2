using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Extensions.Mapping;

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