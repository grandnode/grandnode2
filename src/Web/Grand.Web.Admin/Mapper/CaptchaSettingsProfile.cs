using AutoMapper;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;
using Grand.Web.Common.Security.Captcha;

namespace Grand.Web.Admin.Mapper
{
    public class CaptchaSettingsProfile : Profile, IAutoMapperProfile
    {
        public CaptchaSettingsProfile()
        {
            CreateMap<CaptchaSettings, GeneralCommonSettingsModel.SecuritySettingsModel>()
                .ForMember(dest => dest.CaptchaEnabled, mo => mo.MapFrom(p => p.Enabled))
                .ForMember(dest => dest.CaptchaShowOnLoginPage, mo => mo.MapFrom(p => p.ShowOnLoginPage))
                .ForMember(dest => dest.CaptchaShowOnRegistrationPage, mo => mo.MapFrom(p => p.ShowOnRegistrationPage))
                .ForMember(dest => dest.CaptchaShowOnPasswordRecoveryPage, mo => mo.MapFrom(p => p.ShowOnPasswordRecoveryPage))
                .ForMember(dest => dest.CaptchaShowOnContactUsPage, mo => mo.MapFrom(p => p.ShowOnContactUsPage))
                .ForMember(dest => dest.CaptchaShowOnEmailWishlistToFriendPage, mo => mo.MapFrom(p => p.ShowOnEmailWishlistToFriendPage))
                .ForMember(dest => dest.CaptchaShowOnEmailProductToFriendPage, mo => mo.MapFrom(p => p.ShowOnEmailProductToFriendPage))
                .ForMember(dest => dest.CaptchaShowOnAskQuestionPage, mo => mo.MapFrom(p => p.ShowOnAskQuestionPage))
                .ForMember(dest => dest.CaptchaShowOnBlogCommentPage, mo => mo.MapFrom(p => p.ShowOnBlogCommentPage))
                .ForMember(dest => dest.CaptchaShowOnArticleCommentPage, mo => mo.MapFrom(p => p.ShowOnArticleCommentPage))
                .ForMember(dest => dest.CaptchaShowOnNewsCommentPage, mo => mo.MapFrom(p => p.ShowOnNewsCommentPage))
                .ForMember(dest => dest.CaptchaShowOnProductReviewPage, mo => mo.MapFrom(p => p.ShowOnProductReviewPage))
                .ForMember(dest => dest.CaptchaShowOnApplyVendorPage, mo => mo.MapFrom(p => p.ShowOnApplyVendorPage))
                .ForMember(dest => dest.CaptchaShowOnVendorReviewPage, mo => mo.MapFrom(p => p.ShowOnVendorReviewPage))
                .ForMember(dest => dest.ReCaptchaVersion, mo => mo.MapFrom(p => p.ReCaptchaVersion))
                .ForMember(dest => dest.ReCaptchaPublicKey, mo => mo.MapFrom(p => p.ReCaptchaPublicKey))
                .ForMember(dest => dest.ReCaptchaPrivateKey, mo => mo.MapFrom(p => p.ReCaptchaPrivateKey))
                .ForMember(dest => dest.ReCaptchaScore, mo => mo.MapFrom(p => p.ReCaptchaScore))
                .ForMember(dest => dest.ReCaptchaTheme, mo => mo.MapFrom(p => p.ReCaptchaTheme))
                .ForMember(dest => dest.AvailableReCaptchaVersions, mo => mo.Ignore())
                .ForMember(dest => dest.UserFields, mo => mo.Ignore());

            CreateMap<GeneralCommonSettingsModel.SecuritySettingsModel, CaptchaSettings>()
                .ForMember(dest => dest.Enabled, mo => mo.MapFrom(p => p.CaptchaEnabled))
                .ForMember(dest => dest.ShowOnLoginPage, mo => mo.MapFrom(p => p.CaptchaShowOnLoginPage))
                .ForMember(dest => dest.ShowOnRegistrationPage, mo => mo.MapFrom(p => p.CaptchaShowOnRegistrationPage))
                .ForMember(dest => dest.ShowOnPasswordRecoveryPage, mo => mo.MapFrom(p => p.CaptchaShowOnPasswordRecoveryPage))
                .ForMember(dest => dest.ShowOnContactUsPage, mo => mo.MapFrom(p => p.CaptchaShowOnContactUsPage))
                .ForMember(dest => dest.ShowOnEmailWishlistToFriendPage, mo => mo.MapFrom(p => p.CaptchaShowOnEmailWishlistToFriendPage))
                .ForMember(dest => dest.ShowOnEmailProductToFriendPage, mo => mo.MapFrom(p => p.CaptchaShowOnEmailProductToFriendPage))
                .ForMember(dest => dest.ShowOnAskQuestionPage, mo => mo.MapFrom(p => p.CaptchaShowOnAskQuestionPage))
                .ForMember(dest => dest.ShowOnBlogCommentPage, mo => mo.MapFrom(p => p.CaptchaShowOnBlogCommentPage))
                .ForMember(dest => dest.ShowOnArticleCommentPage, mo => mo.MapFrom(p => p.CaptchaShowOnArticleCommentPage))
                .ForMember(dest => dest.ShowOnNewsCommentPage, mo => mo.MapFrom(p => p.CaptchaShowOnNewsCommentPage))
                .ForMember(dest => dest.ShowOnProductReviewPage, mo => mo.MapFrom(p => p.CaptchaShowOnProductReviewPage))
                .ForMember(dest => dest.ShowOnApplyVendorPage, mo => mo.MapFrom(p => p.CaptchaShowOnApplyVendorPage))
                .ForMember(dest => dest.ShowOnVendorReviewPage, mo => mo.MapFrom(p => p.CaptchaShowOnVendorReviewPage))
                .ForMember(dest => dest.ReCaptchaVersion, mo => mo.MapFrom(p => p.ReCaptchaVersion))
                .ForMember(dest => dest.ReCaptchaPublicKey, mo => mo.MapFrom(p => p.ReCaptchaPublicKey))
                .ForMember(dest => dest.ReCaptchaPrivateKey, mo => mo.MapFrom(p => p.ReCaptchaPrivateKey));
        }

        public int Order => 0;
    }
}