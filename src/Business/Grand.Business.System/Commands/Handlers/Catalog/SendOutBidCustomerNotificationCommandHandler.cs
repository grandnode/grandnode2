using Grand.Business.Catalog.Commands.Models;
using Grand.Business.Messages.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Catalog
{
    public class SendOutBidCustomerNotificationCommandHandler : IRequestHandler<SendOutBidCustomerCommand, bool>
    {
        private readonly IMessageProviderService _messageProviderService;

        public SendOutBidCustomerNotificationCommandHandler(IMessageProviderService messageProviderService)
        {
            _messageProviderService = messageProviderService;
        }

        public async Task<bool> Handle(SendOutBidCustomerCommand request, CancellationToken cancellationToken)
        {
            await _messageProviderService.SendOutBidCustomerMessage(request.Product, request.Language.Id, request.Bid);
            return true;
        }
    }
}
