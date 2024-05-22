using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers;

public class PasswordRecoverySendCommandHandler : IRequestHandler<PasswordRecoverySendCommand, bool>
{
    private readonly IMessageProviderService _messageProviderService;
    private readonly ICustomerService _customerService;

    public PasswordRecoverySendCommandHandler(
        ICustomerService customerService,
        IMessageProviderService messageProviderService)
    {
        _customerService = customerService;
        _messageProviderService = messageProviderService;
    }

    public async Task<bool> Handle(PasswordRecoverySendCommand request, CancellationToken cancellationToken)
    {
        //save token and current date
        var passwordRecoveryToken = Guid.NewGuid();
        await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.PasswordRecoveryToken,
            passwordRecoveryToken.ToString());
        DateTime? generatedDateTime = DateTime.UtcNow;
        await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.PasswordRecoveryTokenDateGenerated,
            generatedDateTime);

        //send email
        await _messageProviderService.SendCustomerPasswordRecoveryMessage(request.Customer, request.Store,
            request.Language.Id);

        return true;
    }
}