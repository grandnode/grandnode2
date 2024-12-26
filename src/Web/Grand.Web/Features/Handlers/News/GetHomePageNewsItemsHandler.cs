﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.News;
using Grand.Web.Models.Media;
using Grand.Web.Models.News;
using MediatR;

namespace Grand.Web.Features.Handlers.News;

public class GetHomePageNewsItemsHandler : IRequestHandler<GetHomePageNewsItems, HomePageNewsItemsModel>
{
    private readonly ICacheBase _cacheBase;
    private readonly IDateTimeService _dateTimeService;
    private readonly MediaSettings _mediaSettings;
    private readonly INewsService _newsService;

    private readonly NewsSettings _newsSettings;
    private readonly IPictureService _pictureService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public GetHomePageNewsItemsHandler(ICacheBase cacheBase, IWorkContextAccessor workContextAccessor,
        INewsService newsService, IDateTimeService dateTimeService, IPictureService pictureService,
        ITranslationService translationService, NewsSettings newsSettings, MediaSettings mediaSettings)
    {
        _cacheBase = cacheBase;
        _workContextAccessor = workContextAccessor;
        _newsService = newsService;
        _dateTimeService = dateTimeService;
        _pictureService = pictureService;
        _translationService = translationService;
        _newsSettings = newsSettings;
        _mediaSettings = mediaSettings;
    }

    public async Task<HomePageNewsItemsModel> Handle(GetHomePageNewsItems request, CancellationToken cancellationToken)
    {
        var cacheKey = string.Format(CacheKeyConst.HOMEPAGE_NEWSMODEL_KEY, _workContextAccessor.WorkContext.WorkingLanguage.Id,
            _workContextAccessor.WorkContext.CurrentStore.Id);
        var model = await _cacheBase.GetAsync(cacheKey, async () =>
        {
            var newsItems =
                await _newsService.GetAllNews(_workContextAccessor.WorkContext.CurrentStore.Id, 0, _newsSettings.MainPageNewsCount);
            var hpnitemodel = new HomePageNewsItemsModel();
            foreach (var item in newsItems)
            {
                var newsModel = await PrepareNewsItemModel(item);
                hpnitemodel.NewsItems.Add(newsModel);
            }

            return hpnitemodel;
        });

        return model;
    }

    private async Task<HomePageNewsItemsModel.NewsItemModel> PrepareNewsItemModel(NewsItem newsItem)
    {
        var model = new HomePageNewsItemsModel.NewsItemModel {
            Id = newsItem.Id,
            SeName = newsItem.GetSeName(_workContextAccessor.WorkContext.WorkingLanguage.Id),
            Title = newsItem.GetTranslation(x => x.Title, _workContextAccessor.WorkContext.WorkingLanguage.Id),
            Short = newsItem.GetTranslation(x => x.Short, _workContextAccessor.WorkContext.WorkingLanguage.Id),
            Full = newsItem.GetTranslation(x => x.Full, _workContextAccessor.WorkContext.WorkingLanguage.Id),
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