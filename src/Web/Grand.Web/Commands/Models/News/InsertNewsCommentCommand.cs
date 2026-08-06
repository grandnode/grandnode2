using Grand.Domain.News;
using Grand.Web.Models.News;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.News;

public class InsertNewsCommentCommand : IRequest<NewsComment>
{
    public NewsItem NewsItem { get; set; }
    public AddNewsCommentModel Model { get; set; }
}