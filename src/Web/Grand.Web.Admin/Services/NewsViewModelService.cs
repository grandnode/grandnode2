using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.News;
using Grand.Domain.Seo;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.News;
using Grand.Web.Common.Extensions;

namespace Grand.Web.Admin.Services;

public class NewsViewModelService : INewsViewModelService
{
    #region Constructors

    public NewsViewModelService(INewsService newsService,
        IDateTimeService dateTimeService,
        ITranslationService translationService,
        ISlugService slugService,
        IPictureService pictureService,
        ILanguageService languageService,
        ICustomerService customerService,
        SeoSettings seoSettings)
    {
        _newsService = newsService;
        _dateTimeService = dateTimeService;
        _translationService = translationService;
        _slugService = slugService;
        _pictureService = pictureService;
        _languageService = languageService;
        _customerService = customerService;
        _seoSettings = seoSettings;
    }

    #endregion

    public virtual async Task<(IEnumerable<NewsItemModel> newsItemModels, int totalCount)> PrepareNewsItemModel(
        NewsItemListModel model, int pageIndex, int pageSize)
    {
        var news = await _newsService.GetAllNews(model.SearchStoreId, pageIndex - 1, pageSize, true, true);
        return (news.Select(x =>
        {
            var m = x.ToModel(_dateTimeService);
            m.Full = "";
            m.CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
            m.Comments = x.CommentCount;
            return m;
        }), news.TotalCount);
    }

    public virtual async Task<NewsItem> InsertNewsItemModel(NewsItemModel model)
    {
        var newsItem = model.ToEntity(_dateTimeService);
        await _newsService.InsertNews(newsItem);

        var seName = await newsItem.ValidateSeName(model.SeName, model.Title, true, _seoSettings, _slugService,
            _languageService);
        newsItem.SeName = seName;
        newsItem.Locales =
            await model.Locales.ToTranslationProperty(newsItem, x => x.Title, _seoSettings, _slugService,
                _languageService);
        await _newsService.UpdateNews(newsItem);
        //search engine name
        await _slugService.SaveSlug(newsItem, seName, "");

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(newsItem.PictureId, newsItem.Title);
        return newsItem;
    }

    public virtual async Task<NewsItem> UpdateNewsItemModel(NewsItem newsItem, NewsItemModel model)
    {
        var prevPictureId = newsItem.PictureId;
        newsItem = model.ToEntity(newsItem, _dateTimeService);
        var seName = await newsItem.ValidateSeName(model.SeName, model.Title, true, _seoSettings, _slugService,
            _languageService);
        newsItem.SeName = seName;
        newsItem.Locales =
            await model.Locales.ToTranslationProperty(newsItem, x => x.Title, _seoSettings, _slugService,
                _languageService);
        await _newsService.UpdateNews(newsItem);

        //search engine name
        await _slugService.SaveSlug(newsItem, seName, "");

        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != newsItem.PictureId)
        {
            var prevPicture = await _pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await _pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(newsItem.PictureId, newsItem.Title);
        return newsItem;
    }

    public virtual async Task<(IEnumerable<NewsCommentModel> newsCommentModels, int totalCount)>
        PrepareNewsCommentModel(string filterByNewsItemId, int pageIndex, int pageSize)
    {
        IList<NewsComment> comments;
        if (!string.IsNullOrEmpty(filterByNewsItemId))
        {
            //filter comments by news item
            var newsItem = await _newsService.GetNewsById(filterByNewsItemId);
            comments = newsItem.NewsComments.OrderBy(bc => bc.CreatedOnUtc).ToList();
        }
        else
        {
            //load all news comments
            comments = await _newsService.GetAllComments("");
        }

        var items = new List<NewsCommentModel>();
        foreach (var newsComment in comments.PagedForCommand(pageIndex, pageSize))
        {
            var commentModel = new NewsCommentModel {
                Id = newsComment.Id,
                NewsItemId = newsComment.NewsItemId,
                NewsItemTitle = (await _newsService.GetNewsById(newsComment.NewsItemId))?.Title,
                CustomerId = newsComment.CustomerId
            };
            var customer = await _customerService.GetCustomerById(newsComment.CustomerId);
            commentModel.CustomerInfo = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _translationService.GetResource("Admin.Customers.Guest");
            commentModel.CreatedOn = _dateTimeService.ConvertToUserTime(newsComment.CreatedOnUtc, DateTimeKind.Utc);
            commentModel.CommentTitle = newsComment.CommentTitle;
            commentModel.CommentText = FormatText.ConvertText(newsComment.CommentText);
            items.Add(commentModel);
        }

        return (items, comments.Count);
    }

    public virtual async Task CommentDelete(NewsComment model)
    {
        var newsItem = await _newsService.GetNewsById(model.NewsItemId);
        var comment = newsItem.NewsComments.FirstOrDefault(x => x.Id == model.Id);
        if (comment == null)
            throw new ArgumentException("No comment found with the specified id");

        newsItem.NewsComments.Remove(comment);
        //update totals
        newsItem.CommentCount = newsItem.NewsComments.Count;
        await _newsService.UpdateNews(newsItem);
    }

    #region Fields

    private readonly INewsService _newsService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ITranslationService _translationService;
    private readonly ISlugService _slugService;
    private readonly IPictureService _pictureService;
    private readonly ILanguageService _languageService;
    private readonly ICustomerService _customerService;
    private readonly SeoSettings _seoSettings;

    #endregion
}