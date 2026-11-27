using Grand.Web.Models.News;
using Grand.Mediator;

namespace Grand.Web.Features.Models.News;

public class GetNewsItemList : IRequest<NewsItemListModel>
{
    public required NewsPagingFilteringModel Command { get; set; }
}