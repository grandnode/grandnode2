using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class SalesEmployeeMappingExtensions
{
    public static SalesEmployeeModel ToModel(this SalesEmployee entity)
    {
        return entity.MapTo<SalesEmployee, SalesEmployeeModel>();
    }

    public static SalesEmployee ToEntity(this SalesEmployeeModel model)
    {
        return model.MapTo<SalesEmployeeModel, SalesEmployee>();
    }

    public static SalesEmployee ToEntity(this SalesEmployeeModel model, SalesEmployee destination)
    {
        return model.MapTo(destination);
    }
}