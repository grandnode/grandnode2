using AutoMapper;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Catalog;


namespace Grand.Web.Admin.Mapper
{
    public class ProductReviewProfile : Profile, IAutoMapperProfile
    {
        public ProductReviewProfile()
        {
            CreateMap<ProductReviewModel, ProductReview>()
                .ForMember(dest => dest.HelpfulYesTotal, mo => mo.Ignore())
                .ForMember(dest => dest.HelpfulNoTotal, mo => mo.Ignore())
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.ProductReviewHelpfulnessEntries, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}