using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Media;
using Grand.Domain.News;
using Grand.Infrastructure;
using Grand.Web.Features.Models.News;
using Grand.Web.Models.Media;
using Grand.Web.Models.News;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.News
{
    public class GetNewsItemListHandler : IRequestHandler<GetNewsItemList, NewsItemListModel>
    {
        private readonly IWorkContext _workContext;
        private readonly INewsService _newsService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;

        private readonly NewsSettings _newsSettings;
        private readonly MediaSettings _mediaSettings;

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
            var model = new NewsItemListModel();
            model.WorkingLanguageId = _workContext.WorkingLanguage.Id;

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
            var model = new NewsItemListModel.NewsItemModel();
            model.Id = newsItem.Id;
            model.SeName = newsItem.GetSeName(_workContext.WorkingLanguage.Id);
            model.Title = newsItem.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id);
            model.Short = newsItem.GetTranslation(x => x.Short, _workContext.WorkingLanguage.Id);
            model.Full = newsItem.GetTranslation(x => x.Full, _workContext.WorkingLanguage.Id);
            model.CreatedOn = _dateTimeService.ConvertToUserTime(newsItem.StartDateUtc ?? newsItem.CreatedOnUtc, DateTimeKind.Utc);
            //prepare picture model
            if (!string.IsNullOrEmpty(newsItem.PictureId))
            {
                var pictureSize = _mediaSettings.NewsListThumbPictureSize;
                model.PictureModel = new PictureModel
                {
                    Id = newsItem.PictureId,
                    FullSizeImageUrl = await _pictureService.GetPictureUrl(newsItem.PictureId),
                    ImageUrl = await _pictureService.GetPictureUrl(newsItem.PictureId, pictureSize),
                    Title = string.Format(_translationService.GetResource("Media.News.ImageLinkTitleFormat"), newsItem.Title),
                    AlternateText = string.Format(_translationService.GetResource("Media.News.ImageAlternateTextFormat"), newsItem.Title)
                };
            }
            return model;
        }
    }
}
