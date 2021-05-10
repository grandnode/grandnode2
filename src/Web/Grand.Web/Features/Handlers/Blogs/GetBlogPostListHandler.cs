using Grand.Infrastructure;
using Grand.Domain;
using Grand.Domain.Blogs;
using Grand.Domain.Media;
using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.Media;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.Business.Common.Extensions;

namespace Grand.Web.Features.Handlers.Blogs
{
    public class GetBlogPostListHandler : IRequestHandler<GetBlogPostList, BlogPostListModel>
    {
        private readonly IBlogService _blogService;
        private readonly IWorkContext _workContext;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;

        private readonly BlogSettings _blogSettings;
        private readonly MediaSettings _mediaSettings;

        public GetBlogPostListHandler(
            IBlogService blogService,
            IWorkContext workContext,
            IPictureService pictureService,
            ITranslationService translationService,
            IDateTimeService dateTimeService,
            BlogSettings blogSettings,
            MediaSettings mediaSettings)
        {
            _blogService = blogService;
            _workContext = workContext;
            _pictureService = pictureService;
            _translationService = translationService;
            _dateTimeService = dateTimeService;

            _blogSettings = blogSettings;
            _mediaSettings = mediaSettings;
        }
        public async Task<BlogPostListModel> Handle(GetBlogPostList request, CancellationToken cancellationToken)
        {
            var model = new BlogPostListModel();
            model.PagingFilteringContext.Tag = request.Command.Tag;
            model.PagingFilteringContext.Month = request.Command.Month;
            model.PagingFilteringContext.CategorySeName = request.Command.CategorySeName;
            model.WorkingLanguageId = _workContext.WorkingLanguage.Id;
            model.SearchKeyword = request.Command.SearchKeyword;

            if (request.Command.PageSize <= 0) request.Command.PageSize = _blogSettings.PostsPageSize;
            if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;

            DateTime? dateFrom = request.Command.GetFromMonth();
            DateTime? dateTo = request.Command.GetToMonth();

            IPagedList<BlogPost> blogPosts;
            if (string.IsNullOrEmpty(request.Command.CategorySeName))
            {
                if (string.IsNullOrEmpty(request.Command.Tag))
                {
                    blogPosts = await _blogService.GetAllBlogPosts(_workContext.CurrentStore.Id,
                        dateFrom, dateTo, request.Command.PageNumber - 1, request.Command.PageSize, blogPostName: model.SearchKeyword);
                }
                else
                {
                    blogPosts = await _blogService.GetAllBlogPostsByTag(_workContext.CurrentStore.Id,
                        request.Command.Tag, request.Command.PageNumber - 1, request.Command.PageSize);
                }
            }
            else
            {
                var categoryblog = await _blogService.GetBlogCategoryBySeName(request.Command.CategorySeName);
                var categoryId = categoryblog != null ? categoryblog.Id : "";
                blogPosts = await _blogService.GetAllBlogPosts(_workContext.CurrentStore.Id,
                        dateFrom, dateTo, request.Command.PageNumber - 1, request.Command.PageSize, categoryId: categoryId, blogPostName: model.SearchKeyword);
            }
            model.PagingFilteringContext.LoadPagedList(blogPosts);

            foreach (var blogpost in blogPosts)
            {
                var blogPostModel = new BlogPostModel();
                await PrepareBlogPostModel(blogPostModel, blogpost);
                model.BlogPosts.Add(blogPostModel);
            }

            return model;
        }

        private async Task PrepareBlogPostModel(BlogPostModel model, BlogPost blogPost)
        {
            if (blogPost == null)
                throw new ArgumentNullException(nameof(blogPost));

            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.Id = blogPost.Id;
            model.MetaTitle = blogPost.GetTranslation(x => x.MetaTitle, _workContext.WorkingLanguage.Id);
            model.MetaDescription = blogPost.GetTranslation(x => x.MetaDescription, _workContext.WorkingLanguage.Id);
            model.MetaKeywords = blogPost.GetTranslation(x => x.MetaKeywords, _workContext.WorkingLanguage.Id);
            model.SeName = blogPost.GetSeName(_workContext.WorkingLanguage.Id);
            model.Title = blogPost.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id);
            model.Body = blogPost.GetTranslation(x => x.Body, _workContext.WorkingLanguage.Id);
            model.BodyOverview = blogPost.GetTranslation(x => x.BodyOverview, _workContext.WorkingLanguage.Id);
            model.AllowComments = blogPost.AllowComments;
            model.CreatedOn = _dateTimeService.ConvertToUserTime(blogPost.StartDateUtc ?? blogPost.CreatedOnUtc, DateTimeKind.Utc);
            model.Tags = blogPost.ParseTags().ToList();
            model.NumberOfComments = blogPost.CommentCount;
            model.UserFields = blogPost.UserFields;

            //prepare picture model
            await PrepareBlogPostPictureModel(model, blogPost);
        }

        private async Task PrepareBlogPostPictureModel(BlogPostModel model, BlogPost blogPost)
        {
            if (!string.IsNullOrEmpty(blogPost.PictureId))
            {
                var pictureModel = new PictureModel {
                    Id = blogPost.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(blogPost.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(blogPost.PictureId, _mediaSettings.BlogThumbPictureSize),
                    Title = string.Format(_translationService.GetResource("Media.Blog.ImageLinkTitleFormat"), blogPost.Title),
                    AlternateText = string.Format(_translationService.GetResource("Media.Blog.ImageAlternateTextFormat"), blogPost.Title)
                };

                model.PictureModel = pictureModel;
            }
        }
    }
}
