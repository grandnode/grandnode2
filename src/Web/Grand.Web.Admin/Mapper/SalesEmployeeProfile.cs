using AutoMapper;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Customers;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Mapper
{
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
}