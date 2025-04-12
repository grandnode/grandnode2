using AutoMapper;
using Grand.Domain.Pages;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Layouts;

namespace Grand.Web.AdminShared.Mapper;

public class PageLayoutProfile : Profile, IAutoMapperProfile
{
    public PageLayoutProfile()
    {
        CreateMap<PageLayout, PageLayoutModel>();
        CreateMap<PageLayoutModel, PageLayout>()
            .ForMember(dest => dest.Id, mo => mo.Ignore());
    }

    public int Order => 0;
}