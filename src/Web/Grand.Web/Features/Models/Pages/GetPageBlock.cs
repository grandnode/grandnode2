using Grand.Web.Models.Pages;
using MediatR;

namespace Grand.Web.Features.Models.Pages
{
    public class GetPageBlock : IRequest<PageModel>
    {
        public string SystemName { get; set; }
        public string PageId { get; set; }
        public string Password { get; set; } 
    }
}
