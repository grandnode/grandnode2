using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;

namespace Grand.Business.Customers.Queries.Handlers;

public class GetPasswordIsExpiredQueryHandler : IRequestHandler<GetPasswordIsExpiredQuery, bool>
{
    private readonly CustomerSettings _customerSettings;
    private readonly IGroupService _groupService;
    private readonly OidcLoginConfig _loginConfig;

    public GetPasswordIsExpiredQueryHandler(IGroupService groupService, CustomerSettings customerSettings,
        OidcLoginConfig loginConfig)
    {
        _groupService = groupService;
        _customerSettings = customerSettings;
        _loginConfig = loginConfig;
    }

    public async Task<bool> Handle(GetPasswordIsExpiredQuery request, CancellationToken cancellationToken)
    {
        if (request.Customer == null)
            throw new ArgumentNullException(nameof(request.Customer));

        //in ProviderOnly the identity provider owns passwords: an expiry would send the account to a change-password page it cannot use
        if (_loginConfig?.ProviderOnly == true)
            return false;

        //user without email don't have a password
        if (string.IsNullOrEmpty(request.Customer.Email))
            return false;

        //password lifetime is disabled for user
        var customerGroups = await _groupService.GetAllByIds(request.Customer.Groups.ToArray());

        if (!customerGroups.Any(role => role.Active && role.EnablePasswordLifetime))
            return false;

        //setting disabled for all
        if (_customerSettings.PasswordLifetime == 0)
            return false;

        var currentLifetime = !request.Customer.PasswordChangeDateUtc.HasValue
            ? int.MaxValue
            : (DateTime.UtcNow - request.Customer.PasswordChangeDateUtc.Value).Days;

        return currentLifetime >= _customerSettings.PasswordLifetime;
    }
}