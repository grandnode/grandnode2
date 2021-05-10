using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Customers;

namespace Grand.Web.Admin.Mapper
{
    public class CustomerActionProfile : Profile, IAutoMapperProfile
    {
        public CustomerActionProfile()
        {
            CreateMap<CustomerAction, CustomerActionModel>()
                .ForMember(dest => dest.MessageTemplates, mo => mo.Ignore())
                .ForMember(dest => dest.Banners, mo => mo.Ignore());

            CreateMap<CustomerActionModel, CustomerAction>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());

            CreateMap<CustomerAction.ActionCondition, CustomerActionConditionModel>()
                .ForMember(dest => dest.CustomerActionConditionType, mo => mo.Ignore());
            CreateMap<CustomerActionConditionModel, CustomerAction.ActionCondition>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}