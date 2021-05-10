using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Mapper
{
    public class ContactUsProfile : Profile, IAutoMapperProfile
    {
        public ContactUsProfile()
        {
            CreateMap<ContactUs, ContactFormModel>()
                .ForMember(dest => dest.CreatedOn, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}