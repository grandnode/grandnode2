using MediatR;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Commands.Models.Common
{
    public class PopupInteractiveCommand : IRequest<IList<string>>
    {
        public IFormCollection Form { get; set; }
    }
}
