using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Extensions;
using Grand.Web.Events.Cache;
using Grand.Web.Features.Models.News;
using Grand.Web.Models.Media;
using Grand.Web.Models.News;
using Grand.Mediator;
using Grand.Web.Common.Localization;

namespace Grand.Web.Features.Handlers.News;

public class GetHomePageNewsItemsHandler : IRequestHandler<GetHomePageNewsItems, HomePageNewsItemsModel>
{
    private readonly AccessControlConfig _accessControlConfig;
    private readonly ICacheBase _cacheBase;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPictureService _pictureService;
    private readonly IRequestTranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly INewsService _newsService;
    private readonly MediaSettings _mediaSettings;
    private readonly NewsSettings _newsSettings;

    private string WorkingLanguageId => _contextAccessor.WorkContext.WorkingLanguage.Id;

    public GetHomePageNewsItemsHandler(ICacheBase cacheBase, IContextAccessor contextAccessor,
        INewsService newsService, IDateTimeService dateTimeService, IPictureService pictureService,
        IRequestTranslationService translationService, NewsSettings newsSettings, MediaSettings mediaSettings,
        AccessControlConfig accessControlConfig)
    {
        _cacheBase = cacheBase;
        _contextAccessor = contextAccessor;
        _newsService = newsService;
        _dateTimeService = dateTimeService;
        _pictureService = pictureService;
        _translationService = translationService;
        _newsSettings = newsSettings;
        _mediaSettings = mediaSettings;
        _accessControlConfig = accessControlConfig;
    }

    public async Task<HomePageNewsItemsModel> Handle(GetHomePageNewsItems request, CancellationToken cancellationToken)
    {
        var access = _contextAccessor.CatalogAccess();
        //with the ACL off every customer sees the same news, so the groups stay out of the key
        var groupIds = _accessControlConfig.IgnoreAcl ? "" : string.Join(",", access.Customer.GetCustomerGroupIds());
        var cacheKey = string.Format(CacheKeyConst.HOMEPAGE_NEWSMODEL_KEY, WorkingLanguageId, access.StoreId, groupIds);
        var model = await _cacheBase.GetAsync(cacheKey, async () =>
        {
            var newsItems = await _newsService.GetAllNews(access, 0, _newsSettings.MainPageNewsCount);
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
            SeName = newsItem.GetSeName(WorkingLanguageId),
            Title = newsItem.GetTranslation(x => x.Title, WorkingLanguageId),
            Short = newsItem.GetTranslation(x => x.Short, WorkingLanguageId),
            Full = newsItem.GetTranslation(x => x.Full, WorkingLanguageId),
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