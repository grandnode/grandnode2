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
    private readonly MediaSettings _mediaSettings;
    private readonly INewsService _newsService;

    private readonly NewsSettings _newsSettings;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public GetNewsItemListHandler(IWorkContext workContext,
        INewsService newsService, IDateTimeService dateTimeService, IPictureService pictureService,
        ITranslationService translationService, NewsSettings newsSettings, MediaSettings mediaSettings)
    {
        _workContext = workContext;
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
            WorkingLanguageId = _workContext.WorkingLanguage.Id
        };

        if (request.Command.PageSize <= 0) request.Command.PageSize = _newsSettings.NewsArchivePageSize;
        if (request.Command.PageNumber <= 0) request.Command.PageNumber = 1;

        var newsItems = await _newsService.GetAllNews(_workContext.CurrentStore.Id,
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
            SeName = newsItem.GetSeName(_workContext.WorkingLanguage.Id),
            Title = newsItem.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id),
            Short = newsItem.GetTranslation(x => x.Short, _workContext.WorkingLanguage.Id),
            Full = newsItem.GetTranslation(x => x.Full, _workContext.WorkingLanguage.Id),
            CreatedOn = _dateTimeService.ConvertToUserTime(newsItem.StartDateUtc ?? newsItem.CreatedOnUtc,
                DateTimeKind.Utc)
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