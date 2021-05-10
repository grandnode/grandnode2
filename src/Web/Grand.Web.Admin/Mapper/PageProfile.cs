using AutoMapper;
using Grand.Business.Common.Extensions;
using Grand.Domain.Pages;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Pages;
using System.Linq;

namespace Grand.Web.Admin.Mapper
{
    public class PageProfile : Profile, IAutoMapperProfile
    {
        public PageProfile()
        {
            CreateMap<Page, PageModel>()
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AvailablePageLayouts, mo => mo.Ignore())
                .ForMember(dest => dest.Url, mo => mo.Ignore())
                .ForMember(dest => dest.SeName, mo => mo.MapFrom(src => src.GetSeName("", true)));

            CreateMap<PageModel, Page>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.LimitedToGroups, mo => mo.MapFrom(x => x.CustomerGroups != null && x.CustomerGroups.Any()))
                .ForMember(dest => dest.LimitedToStores, mo => mo.MapFrom(x => x.Stores != null && x.Stores.Any()));
        }

        public int Order => 0;
    }
}