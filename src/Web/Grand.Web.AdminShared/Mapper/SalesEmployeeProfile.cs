using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Customers;

namespace Grand.Web.AdminShared.Mapper;

public class SalesEmployeeProfile : Profile, IAutoMapperProfile
{
    public SalesEmployeeProfile()
    {
        CreateMap<SalesEmployee, SalesEmployeeModel>();

        CreateMap<SalesEmployeeModel, SalesEmployee>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}