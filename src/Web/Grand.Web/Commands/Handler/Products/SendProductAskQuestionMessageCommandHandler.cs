using Grand.Business.Core.Interfaces.Messages;
using Grand.SharedKernel.Extensions;
using Grand.Web.Commands.Models.Products;
using MediatR;

namespace Grand.Web.Commands.Handler.Products
{
    public class SendProductAskQuestionMessageCommandHandler : IRequestHandler<SendProductAskQuestionMessageCommand, bool>
    {
        private readonly IMessageProviderService _messageProviderService;

        public SendProductAskQuestionMessageCommandHandler(IMessageProviderService messageProviderService)
        {
            _messageProviderService = messageProviderService;
        }

        public async Task<bool> Handle(SendProductAskQuestionMessageCommand request, CancellationToken cancellationToken)
        {
            await _messageProviderService.SendProductQuestionMessage(request.Customer, request.Store,
                               request.Language.Id, request.Product, request.Model.Email, request.Model.FullName, request.Model.Phone,
                               FormatText.ConvertText(request.Model.Message), request.RemoteIpAddress);

            return true;
        }
    }
}
