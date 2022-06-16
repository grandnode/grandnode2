using Grand.Business.Common.Services.Security;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Customer;
using MediatR;


namespace Grand.Web.Commands.Models.Customers
{
    public class EmailCodeSendCommand : IRequest<bool>
    {
        public LoginWithEmailCodeModel Model { get; set; }

        public Customer Customer { get; set; }
        public Store Store { get; set; }
        public Language Language { get; set; }

        public int MinutesToExpire { get; set; }

        public HashedPasswordFormat HashedPasswordFormat { get; set; } = HashedPasswordFormat.SHA1;
        public EncryptionService EncryptionService { get; set; } = new EncryptionService();
    }
}
