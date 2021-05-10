using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Commands.Handler.Customers
{
    public class PasswordRecoverySendCommandHandler : IRequestHandler<PasswordRecoverySendCommand, bool>
    {
        private readonly IUserFieldService _userFieldService;
        private readonly IMessageProviderService _messageProviderService;

        public PasswordRecoverySendCommandHandler(
            IUserFieldService userFieldService,
            IMessageProviderService messageProviderService)
        {
            _userFieldService = userFieldService;
            _messageProviderService = messageProviderService;
        }

        public async Task<bool> Handle(PasswordRecoverySendCommand request, CancellationToken cancellationToken)
        {
            //save token and current date
            var passwordRecoveryToken = Guid.NewGuid();
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.PasswordRecoveryToken, passwordRecoveryToken.ToString());
            DateTime? generatedDateTime = DateTime.UtcNow;
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.PasswordRecoveryTokenDateGenerated, generatedDateTime);

            //send email
            await _messageProviderService.SendCustomerPasswordRecoveryMessage(request.Customer, request.Store, request.Language.Id);

            return true;
        }
    }
}
