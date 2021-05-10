using AutoMapper;
using Grand.Domain.Messages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Mapper
{
    public class EmailAccountProfile : Profile, IAutoMapperProfile
    {
        public EmailAccountProfile()
        {
            CreateMap<EmailAccount, EmailAccountModel>()
                .ForMember(dest => dest.Password, mo => mo.Ignore())
                .ForMember(dest => dest.IsDefaultEmailAccount, mo => mo.Ignore())
                .ForMember(dest => dest.SendTestEmailTo, mo => mo.Ignore());

            CreateMap<EmailAccountModel, EmailAccount>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Password, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}