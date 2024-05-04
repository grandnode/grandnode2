using AutoMapper;
using Grand.Business.Core.Extensions;
using Grand.Infrastructure.Mapper;
using Grand.Web.Vendor.Models.Vendor;

namespace Grand.Web.Vendor.Mapper;

public class VendorProfile : Profile, IAutoMapperProfile
{
    public VendorProfile()
    {
        CreateMap<Domain.Vendors.Vendor, VendorModel>()
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.UserFields, mo => mo.Ignore())
            .ForMember(dest => dest.SeName, mo => mo.MapFrom(src => src.GetSeName("", true)));

        CreateMap<VendorModel, Domain.Vendors.Vendor>()
            .ForMember(dest => dest.Id, mo => mo.Ignore())
            .ForMember(dest => dest.Locales, mo => mo.Ignore())
            .ForMember(dest => dest.Coordinates, mo => mo.Ignore())
            .ForMember(dest => dest.Active, mo => mo.Ignore())
            .ForMember(dest => dest.Commission, mo => mo.Ignore())
            .ForMember(dest => dest.DisplayOrder, mo => mo.Ignore())
            .ForMember(dest => dest.AdminComment, mo => mo.Ignore())
            .ForMember(dest => dest.AppliedDiscounts, mo => mo.Ignore())
            .ForMember(dest => dest.PictureId, mo => mo.Ignore())
            .ForMember(dest => dest.VendorNotes, mo => mo.Ignore())
            .ForMember(dest => dest.NotApprovedRatingSum, mo => mo.Ignore())
            .ForMember(dest => dest.ApprovedRatingSum, mo => mo.Ignore())
            .ForMember(dest => dest.ApprovedTotalReviews, mo => mo.Ignore())
            .ForMember(dest => dest.NotApprovedTotalReviews, mo => mo.Ignore())
            .ForMember(dest => dest.AllowCustomerReviews, mo => mo.Ignore())
            .ForMember(dest => dest.Deleted, mo => mo.Ignore());
    }

    public int Order => 0;
}