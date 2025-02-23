using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Web.Features.Models.News;
using Grand.Web.Models.Media;
using Grand.Web.Models.News;
using MediatR;

namespace Grand.Web.Features.Handlers.News;

public class GetNewsItemListHandler : IRequestHandler<GetNewsItemList, NewsItemListModel>
{
    private readonly IDateTimeService _dateTimeService;
    private readonly INewsService _newsService;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly MediaSettings _mediaSettings;
    private readonly NewsSettings _newsSettings;

    private string WorkingLanguageId => _contextAccessor.WorkContext.WorkingLanguage.Id;

    public GetNewsItemListHandler(IContextAccessor contextAccessor,
        INewsService newsService, IDateTimeService dateTimeService, IPictureService pictureService,
        ITranslationService translationService, NewsSettings newsSettings, MediaSettings mediaSettings)
    {
        _contextAccessor = contextAccessor;
        _newsService = newsService;
        _dateTimeService = dateTimeService;
        _pictureService = pictureService;
        _translationService = translationService;
        _newsSettings = newsSettings;
        _mediaSettings = mediaSettings;
    }

    public async Task<NewsItemListModel> Handle(GetNewsItemList request, CancellationToken cancellationToken)
    {
        var model = new NewsItemListModel {
            WorkingLanguageId = WorkingLanguageId
        };

        if (request.Command.PageSize <= 0) request.Command.PageSize = _newsSettings.NewsArchivePageSize;
        if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;

        var newsItems = await _newsService.GetAllNews(_contextAccessor.StoreContext.CurrentStore.Id,
            request.Command.PageNumber - 1, request.Command.PageSize);
        model.PagingFilteringContext.LoadPagedList(newsItems);
        foreach (var item in newsItems)
        {
            var newsModel = await PrepareNewsItemModel(item);
            model.NewsItems.Add(newsModel);
        }

        return model;
    }

    private async Task<NewsItemListModel.NewsItemModel> PrepareNewsItemModel(NewsItem newsItem)
    {
        var model = new NewsItemListModel.NewsItemModel {
            Id = newsItem.Id,
            SeName = newsItem.GetSeName(WorkingLanguageId),
            Title = newsItem.GetTranslation(x => x.Title, WorkingLanguageId),
            Short = newsItem.GetTranslation(x => x.Short, WorkingLanguageId),
            Full = newsItem.GetTranslation(x => x.Full, WorkingLanguageId),
            CreatedOn = _dateTimeService.ConvertToUserTime(newsItem.StartDateUtc ?? newsItem.CreatedOnUtc, DateTimeKind.Utc)
        };
        //prepare picture model
        if (string.IsNullOrEmpty(newsItem.PictureId)) return model;

        var pictureSize = _mediaSettings.NewsListThumbPictureSize;
        model.PictureModel = new PictureModel {
            Id = newsItem.PictureId,
            FullSizeImageUrl = await _pictureService.GetPictureUrl(newsItem.PictureId),
            ImageUrl = await _pictureService.GetPictureUrl(newsItem.PictureId, pictureSize),
            Title = string.Format(_translationService.GetResource("Media.News.ImageLinkTitleFormat"), newsItem.Title),
            AlternateText = string.Format(_translationService.GetResource("Media.News.ImageAlternateTextFormat"),
                newsItem.Title)
        };
        return model;
    }
}