using MediatR;

namespace Grand.Business.System.Commands.Models.Common
{
    public class UseFullTextSearchCommand : IRequest<bool>
    {
        public bool UseFullTextSearch { get; set; }
    }
}
