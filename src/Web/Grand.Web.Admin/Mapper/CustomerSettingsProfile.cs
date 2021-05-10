using AutoMapper;
using Grand.Domain.Customers;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Mapper
{
    public class CustomerSettingsProfile : Profile, IAutoMapperProfile
    {
        public CustomerSettingsProfile()
        {
            CreateMap<CustomerSettings, CustomerSettingsModel.CustomersSettingsModel>()
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());
            CreateMap<CustomerSettingsModel.CustomersSettingsModel, CustomerSettings>();
        }

        public int Order => 0;
    }
}