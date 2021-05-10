using Grand.Business.Messages.Interfaces;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Products;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Products
{
    public class SendProductEmailAFriendMessageCommandHandler : IRequestHandler<SendProductEmailAFriendMessageCommand, bool>
    {
        private readonly IMessageProviderService _messageProviderService;

        public SendProductEmailAFriendMessageCommandHandler(IMessageProviderService messageProviderService)
        {
            _messageProviderService = messageProviderService;
        }

        public async Task<bool> Handle(SendProductEmailAFriendMessageCommand request, CancellationToken cancellationToken)
        {
            await _messageProviderService.SendProductEmailAFriendMessage(request.Customer, request.Store,
                               request.Language.Id, request.Product,
                               request.Model.YourEmailAddress, request.Model.FriendEmail,
                               FormatText.ConvertText(request.Model.PersonalMessage));

            return true;
        }
    }
}
