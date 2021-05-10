using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Mapper
{
    public class CustomerGroupProfile : Profile, IAutoMapperProfile
    {
        public CustomerGroupProfile()
        {
            CreateMap<CustomerGroup, CustomerGroupModel>();
            CreateMap<CustomerGroupModel, CustomerGroup>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}