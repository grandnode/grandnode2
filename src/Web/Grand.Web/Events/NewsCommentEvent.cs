using Grand.Domain.News;
using Grand.Web.Models.News;
using MediatR;

namespace Grand.Web.Events;

public class NewsCommentEvent : INotification
{
    public NewsCommentEvent(NewsItem news, AddNewsCommentModel newsComment)
    {
        News = news;
        NewsComment = newsComment;
    }

    public NewsItem News { get; private set; }
    public AddNewsCommentModel NewsComment { get; private set; }
}