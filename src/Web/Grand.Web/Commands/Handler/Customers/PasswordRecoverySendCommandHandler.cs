using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using Grand.Mediator;

namespace Grand.Web.Commands.Handler.Customers;

public class PasswordRecoverySendCommandHandler : IRequestHandler<PasswordRecoverySendCommand, bool>
{
    private readonly ICustomerMessageProviderService _customerMessageProviderService;
    private readonly ICustomerService _customerService;
    private readonly IExternalSignInPolicy _externalSignInPolicy;

    public PasswordRecoverySendCommandHandler(
        ICustomerService customerService,
        ICustomerMessageProviderService customerMessageProviderService,
        IExternalSignInPolicy externalSignInPolicy)
    {
        _externalSignInPolicy = externalSignInPolicy;
        _customerService = customerService;
        _customerMessageProviderService = customerMessageProviderService;
    }

    public async Task<bool> Handle(PasswordRecoverySendCommand request, CancellationToken cancellationToken)
    {
        //an account that signs in only with the identity provider has no password to recover; the page answers as usual
        if (request.Customer == null || !(await _externalSignInPolicy.PasswordSignIn(request.Customer, SignInSurface.Storefront)).Allowed)
            return true;

        //save token and current date
        var passwordRecoveryToken = Guid.NewGuid();
        await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.PasswordRecoveryToken,
            passwordRecoveryToken.ToString());
        DateTime? generatedDateTime = DateTime.UtcNow;
        await _customerService.UpdateUserField(request.Customer, SystemCustomerFieldNames.PasswordRecoveryTokenDateGenerated,
            generatedDateTime);
        //the link in the message leads to the storefront, not to a panel an earlier request came from
        await _customerService.UpdateUserField<string>(request.Customer, SystemCustomerFieldNames.PasswordRecoveryArea,
            null);

        //send email
        await _customerMessageProviderService.SendCustomerPasswordRecoveryMessage(request.Customer, request.Store,
            request.Language.Id, host: request.Host);

        return true;
    }
}