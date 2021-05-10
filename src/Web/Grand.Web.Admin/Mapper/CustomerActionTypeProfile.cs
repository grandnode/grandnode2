using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Mapper
{
    public class CustomerActionTypeProfile : Profile, IAutoMapperProfile
    {
        public CustomerActionTypeProfile()
        {
            CreateMap<CustomerActionTypeModel, CustomerActionType>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.SystemKeyword, mo => mo.Ignore());
            CreateMap<CustomerActionType, CustomerActionTypeModel>();
        }

        public int Order => 0;
    }
}