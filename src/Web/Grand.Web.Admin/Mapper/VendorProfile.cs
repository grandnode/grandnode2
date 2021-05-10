using AutoMapper;
using Grand.Business.Common.Extensions;
using Grand.Infrastructure.Mapper;
using Grand.Domain.Vendors;
using Grand.Web.Admin.Models.Vendors;

namespace Grand.Web.Admin.Mapper
{
    public class VendorProfile : Profile, IAutoMapperProfile
    {
        public VendorProfile()
        {
            CreateMap<Vendor, VendorModel>()
                .ForMember(dest => dest.AssociatedCustomers, mo => mo.Ignore())
                .ForMember(dest => dest.AddVendorNoteMessage, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableDiscounts, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableStores, mo => mo.Ignore())
                .ForMember(dest => dest.SelectedDiscountIds, mo => mo.Ignore())
                .ForMember(dest => dest.SeName, mo => mo.MapFrom(src => src.GetSeName("", true)));

            CreateMap<VendorModel, Vendor>()
                .ForMember(dest => dest.Id, mo => mo.Ignore())
                .ForMember(dest => dest.Locales, mo => mo.Ignore())
                .ForMember(dest => dest.Coordinates, mo => mo.Ignore())
                .ForMember(dest => dest.Active, mo => mo.Ignore())
                .ForMember(dest => dest.Deleted, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}