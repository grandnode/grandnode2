using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Messages;
using MediatR;

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
