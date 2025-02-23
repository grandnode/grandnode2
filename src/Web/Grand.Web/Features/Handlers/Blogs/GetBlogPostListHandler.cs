using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain;
using Grand.Domain.Blogs;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Web.Features.Models.Blogs;
using Grand.Web.Models.Blogs;
using Grand.Web.Models.Media;
using MediatR;

namespace Grand.Web.Features.Handlers.Blogs;

public class GetBlogPostListHandler : IRequestHandler<GetBlogPostList, BlogPostListModel>
{
    private readonly IBlogService _blogService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly MediaSettings _mediaSettings;
    private readonly BlogSettings _blogSettings;

    private string WorkingLanguageId => _contextAccessor.WorkContext.WorkingLanguage.Id;
    private string CurrentStoreId => _contextAccessor.StoreContext.CurrentStore.Id;

    public GetBlogPostListHandler(
        IBlogService blogService,
        IContextAccessor contextAccessor,
        IPictureService pictureService,
        ITranslationService translationService,
        IDateTimeService dateTimeService,
        BlogSettings blogSettings,
        MediaSettings mediaSettings)
    {
        _blogService = blogService;
        _contextAccessor = contextAccessor;
        _pictureService = pictureService;
        _translationService = translationService;
        _dateTimeService = dateTimeService;

        _blogSettings = blogSettings;
        _mediaSettings = mediaSettings;
    }

    public async Task<BlogPostListModel> Handle(GetBlogPostList request, CancellationToken cancellationToken)
    {
        var model = new BlogPostListModel {
            PagingFilteringContext = {
                Tag = request.Command.Tag,
                Month = request.Command.Month,
                CategorySeName = request.Command.CategorySeName
            },
            WorkingLanguageId = WorkingLanguageId,
            SearchKeyword = request.Command.SearchKeyword
        };

        if (request.Command.PageSize <= 0) request.Command.PageSize = _blogSettings.PostsPageSize;
        if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;

        var dateFrom = request.Command.GetFromMonth();
        var dateTo = request.Command.GetToMonth();

        IPagedList<BlogPost> blogPosts;
        if (string.IsNullOrEmpty(request.Command.CategorySeName))
        {
            if (string.IsNullOrEmpty(request.Command.Tag))
                blogPosts = await _blogService.GetAllBlogPosts(CurrentStoreId,
                    dateFrom, dateTo, request.Command.PageNumber - 1, request.Command.PageSize,
                    blogPostName: model.SearchKeyword);
            else
                blogPosts = await _blogService.GetAllBlogPostsByTag(CurrentStoreId,
                    request.Command.Tag, request.Command.PageNumber - 1, request.Command.PageSize);
        }
        else
        {
            var categoryblog = await _blogService.GetBlogCategoryBySeName(request.Command.CategorySeName);
            var categoryId = categoryblog != null ? categoryblog.Id : "";
            blogPosts = await _blogService.GetAllBlogPosts(CurrentStoreId,
                dateFrom, dateTo, request.Command.PageNumber - 1, request.Command.PageSize, categoryId: categoryId,
                blogPostName: model.SearchKeyword);
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
        ArgumentNullException.ThrowIfNull(blogPost);
        ArgumentNullException.ThrowIfNull(model);

        model.Id = blogPost.Id;
        model.MetaTitle = blogPost.GetTranslation(x => x.MetaTitle, WorkingLanguageId);
        model.MetaDescription = blogPost.GetTranslation(x => x.MetaDescription, WorkingLanguageId);
        model.MetaKeywords = blogPost.GetTranslation(x => x.MetaKeywords, WorkingLanguageId);
        model.SeName = blogPost.GetSeName(WorkingLanguageId);
        model.Title = blogPost.GetTranslation(x => x.Title, WorkingLanguageId);
        model.Body = blogPost.GetTranslation(x => x.Body, WorkingLanguageId);
        model.BodyOverview = blogPost.GetTranslation(x => x.BodyOverview, WorkingLanguageId);
        model.AllowComments = blogPost.AllowComments;
        model.CreatedOn =
            _dateTimeService.ConvertToUserTime(blogPost.StartDateUtc ?? blogPost.CreatedOnUtc, DateTimeKind.Utc);
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
            var picture = await _pictureService.GetPictureById(blogPost.PictureId);

            var pictureModel = new PictureModel {
                Id = blogPost.PictureId,
                FullSizeImageUrl = await _pictureService.GetPictureUrl(blogPost.PictureId),
                ImageUrl = await _pictureService.GetPictureUrl(blogPost.PictureId, _mediaSettings.BlogThumbPictureSize),
                Style = picture?.Style,
                ExtraField = picture?.ExtraField,
                //"title" attribute
                Title =
                    picture != null &&
                    !string.IsNullOrEmpty(
                        picture.GetTranslation(x => x.TitleAttribute, WorkingLanguageId))
                        ? picture.GetTranslation(x => x.TitleAttribute, WorkingLanguageId)
                        : string.Format(_translationService.GetResource("Media.Blog.ImageLinkTitleFormat"),
                            blogPost.Title),
                //"alt" attribute
                AlternateText =
                    picture != null &&
                    !string.IsNullOrEmpty(picture.GetTranslation(x => x.AltAttribute, WorkingLanguageId))
                        ? picture.GetTranslation(x => x.AltAttribute, WorkingLanguageId)
                        : string.Format(_translationService.GetResource("Media.Blog.ImageAlternateTextFormat"),
                            blogPost.Title)
            };

            model.PictureModel = pictureModel;
        }
    }
}