using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Mediator;

namespace Grand.Module.ScheduledTasks.Commands.Handlers.Catalog;

public class SendOutBidCustomerNotificationCommandHandler : IRequestHandler<SendOutBidCustomerCommand, bool>
{
    private readonly ICatalogMessageProviderService _catalogMessageProviderService;

    public SendOutBidCustomerNotificationCommandHandler(ICatalogMessageProviderService catalogMessageProviderService)
    {
        _catalogMessageProviderService = catalogMessageProviderService;
    }

    public async Task<bool> Handle(SendOutBidCustomerCommand request, CancellationToken cancellationToken)
    {
        await _catalogMessageProviderService.SendOutBidCustomerMessage(request.Product, request.Language.Id, request.Bid);
        return true;
    }
}