﻿using Grand.Domain.News;
using Grand.Web.Admin.Models.News;

namespace Grand.Web.Admin.Interfaces
{
    public interface INewsViewModelService
    {
        Task<(IEnumerable<NewsItemModel> newsItemModels, int totalCount)> PrepareNewsItemModel(NewsItemListModel model, int pageIndex, int pageSize);
        Task<NewsItem> InsertNewsItemModel(NewsItemModel model);
        Task<NewsItem> UpdateNewsItemModel(NewsItem newsItem, NewsItemModel model);
        Task<(IEnumerable<NewsCommentModel> newsCommentModels, int totalCount)> PrepareNewsCommentModel(string filterByNewsItemId, int pageIndex, int pageSize);
        Task CommentDelete(NewsComment model);
    }
}
