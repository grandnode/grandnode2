using Grand.Business.Core.Interfaces.Messages;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Products;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Products;

public class SendProductAskQuestionMessageCommandHandler : IRequestHandler<SendProductAskQuestionMessageCommand, bool>
{
    private readonly ICatalogMessageProviderService _catalogMessageProviderService;

    public SendProductAskQuestionMessageCommandHandler(ICatalogMessageProviderService catalogMessageProviderService)
    {
        _catalogMessageProviderService = catalogMessageProviderService;
    }

    public async Task<bool> Handle(SendProductAskQuestionMessageCommand request, CancellationToken cancellationToken)
    {
        await _catalogMessageProviderService.SendProductQuestionMessage(request.Customer, request.Store,
            request.Language.Id, request.Product, request.Model.Email, request.Model.FullName, request.Model.Phone,
            FormatText.ConvertText(request.Model.Message), request.RemoteIpAddress, host: request.Host);

        return true;
    }
}