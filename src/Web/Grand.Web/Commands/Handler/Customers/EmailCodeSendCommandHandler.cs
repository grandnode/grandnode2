using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers
{
    public class EmailCodeSendCommandHandler : IRequestHandler<EmailCodeSendCommand, bool>
    {
        private readonly IUserFieldService _userFieldService;
        private readonly IMessageProviderService _messageProviderService;

        public EmailCodeSendCommandHandler(
            IUserFieldService userFieldService,
            IMessageProviderService messageProviderService)
        {
            _userFieldService = userFieldService;
            _messageProviderService = messageProviderService;
        }

        public async Task<bool> Handle(EmailCodeSendCommand request, CancellationToken cancellationToken)
        {
            const int MINUTES_TO_EXPIRE = 10; // This could be moved to a config area so the admin can change it.
    
            // Generate GUID & Timestamp
            request.Customer.LoginCode = (Guid.NewGuid()).ToString(); // Store in model so we can pass it down to the message send process.
            long loginCodeExpiry = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() + (MINUTES_TO_EXPIRE * 60);


            // Encrypt loginCode
            var salt = request.Customer.PasswordSalt;
            var hashedLoginCode = request.EncryptionService.CreatePasswordHash(request.Customer.LoginCode, salt, request.HashedPasswordFormat);

            // Save to Db
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.EmailLoginToken, hashedLoginCode);
            await _userFieldService.SaveField(request.Customer, SystemCustomerFieldNames.EmailLoginTokenExpiry, loginCodeExpiry);
           
            // Send email
            await _messageProviderService.SendCustomerEmailLoginLinkMessage(request.Customer, request.Store, request.Language.Id);
            request.Customer.LoginCode = ""; // Wipe this out! Should be no reference to the unhashed version of this code on the system once we no longer need it.

            return true;
        }
    }
}
