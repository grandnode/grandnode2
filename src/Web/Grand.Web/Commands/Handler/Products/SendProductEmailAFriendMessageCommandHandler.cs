using Grand.Business.Core.Interfaces.Messages;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Products;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Products;

public class SendProductEmailAFriendMessageCommandHandler : IRequestHandler<SendProductEmailAFriendMessageCommand, bool>
{
    private readonly ICatalogMessageProviderService _catalogMessageProviderService;

    public SendProductEmailAFriendMessageCommandHandler(ICatalogMessageProviderService catalogMessageProviderService)
    {
        _catalogMessageProviderService = catalogMessageProviderService;
    }

    public async Task<bool> Handle(SendProductEmailAFriendMessageCommand request, CancellationToken cancellationToken)
    {
        await _catalogMessageProviderService.SendProductEmailAFriendMessage(request.Customer, request.Store,
            request.Language.Id, request.Product,
            request.Model.YourEmailAddress, request.Model.FriendEmail,
            FormatText.ConvertText(request.Model.PersonalMessage), host: request.Host);

        return true;
    }
}