using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Queries.Models;
using Grand.Domain.Customers;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Queries.Handlers
{
    public class GetPasswordIsExpiredQueryHandler : IRequestHandler<GetPasswordIsExpiredQuery, bool>
    {
        private readonly IGroupService _groupService;
        private readonly CustomerSettings _customerSettings;

        public GetPasswordIsExpiredQueryHandler(IGroupService groupService, CustomerSettings customerSettings)
        {
            _groupService = groupService;
            _customerSettings = customerSettings;
        }

        public async Task<bool> Handle(GetPasswordIsExpiredQuery request, CancellationToken cancellationToken)
        {
            if (request.Customer == null)
                throw new ArgumentNullException(nameof(request.Customer));

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

            var currentLifetime = 0;
            if (!request.Customer.PasswordChangeDateUtc.HasValue)
                currentLifetime = int.MaxValue;
            else
                currentLifetime = (DateTime.UtcNow - request.Customer.PasswordChangeDateUtc.Value).Days;

            return currentLifetime >= _customerSettings.PasswordLifetime;
        }
    }
}
