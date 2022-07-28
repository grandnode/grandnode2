using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Domain.Blogs;
using Grand.Domain.Media;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Events.Cache;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.Media;
using MediatR;

namespace Grand.Web.Features.Handlers.Blogs
{
    public class GetHomePageBlogHandler : IRequestHandler<GetHomePageBlog, HomePageBlogItemsModel>
    {
        private readonly IBlogService _blogService;
        private readonly ICacheBase _cacheBase;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;

        private readonly BlogSettings _blogSettings;
        private readonly MediaSettings _mediaSettings;

        public GetHomePageBlogHandler(IBlogService blogService,
            IWorkContext workContext,
            IPictureService pictureService,
            ITranslationService translationService,
            IDateTimeService dateTimeService,
            ICacheBase cacheBase,
            BlogSettings blogSettings,
            MediaSettings mediaSettings)
        {
            _blogService = blogService;
            _workContext = workContext;
            _pictureService = pictureService;
            _translationService = translationService;
            _dateTimeService = dateTimeService;
            _cacheBase = cacheBase;

            _blogSettings = blogSettings;
            _mediaSettings = mediaSettings;
        }

        public async Task<HomePageBlogItemsModel> Handle(GetHomePageBlog request, CancellationToken cancellationToken)
        {
            var cacheKey = string.Format(CacheKeyConst.BLOG_HOMEPAGE_MODEL_KEY,
                _workContext.WorkingLanguage.Id,
                _workContext.CurrentStore.Id);
            var cachedModel = await _cacheBase.GetAsync(cacheKey, async () =>
            {
                var model = new HomePageBlogItemsModel();

                var blogPosts = await _blogService.GetAllBlogPosts(_workContext.CurrentStore.Id,
                        null, null, 0, _blogSettings.HomePageBlogCount);

                foreach (var post in blogPosts)
                {
                    var item = new HomePageBlogItemsModel.BlogItemModel();
                    var description = post.GetTranslation(x => x.BodyOverview, _workContext.WorkingLanguage.Id);
                    item.SeName = post.GetSeName(_workContext.WorkingLanguage.Id);
                    item.Title = post.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id);
                    item.Short = description?.Length > _blogSettings.MaxTextSizeHomePage ? description[.._blogSettings.MaxTextSizeHomePage] : description;
                    item.CreatedOn = _dateTimeService.ConvertToUserTime(post.StartDateUtc ?? post.CreatedOnUtc, DateTimeKind.Utc);
                    item.UserFields = post.UserFields;
                    item.Category = (await _blogService.GetBlogCategoryByPostId(post.Id)).FirstOrDefault()?.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id);

                    //prepare picture model
                    if (!string.IsNullOrEmpty(post.PictureId))
                    {
                        var picture = await _pictureService.GetPictureById(post.PictureId);

                        var pictureModel = new PictureModel
                        {
                            Id = post.PictureId,
                            FullSizeImageUrl = await _pictureService.GetPictureUrl(post.PictureId),
                            ImageUrl = await _pictureService.GetPictureUrl(post.PictureId, _mediaSettings.BlogThumbPictureSize),
                            Title = string.Format(_translationService.GetResource("Media.Blog.ImageLinkTitleFormat"), post.Title),
                            AlternateText = string.Format(_translationService.GetResource("Media.Blog.ImageAlternateTextFormat"), post.Title),
                            Style = picture?.Style,
                            ExtraField = picture?.ExtraField
                        };
                        //"title" attribute
                        pictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.TitleAttribute, _workContext.WorkingLanguage.Id))) ?
                            picture.GetTranslation(x => x.TitleAttribute, _workContext.WorkingLanguage.Id) :
                            string.Format(_translationService.GetResource("Media.Blog.ImageLinkTitleFormat"), post.Title);
                        //"alt" attribute
                        pictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, _workContext.WorkingLanguage.Id))) ?
                            picture.GetTranslation(x => x.AltAttribute, _workContext.WorkingLanguage.Id) :
                            string.Format(_translationService.GetResource("Media.Blog.ImageAlternateTextFormat"), post.Title);

                        item.PictureModel = pictureModel;
                    }
                    model.Items.Add(item);
                }
                return model;
            });

            return cachedModel;
        }
    }
}
