using Grand.Business.Messages.Commands.Models;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Commands.Handlers
{
    public class InsertContactUsCommandHandler : IRequestHandler<InsertContactUsCommand, bool>
    {
        private readonly IRepository<ContactUs> _repository;

        public InsertContactUsCommandHandler(
            IRepository<ContactUs> repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(InsertContactUsCommand request, CancellationToken cancellationToken)
        {
            var contactus = new ContactUs() {
                CreatedOnUtc = DateTime.UtcNow,
                CustomerId = request.CustomerId,
                StoreId = request.StoreId,
                VendorId = request.VendorId,
                Email = request.Email,
                FullName = request.FullName,
                Subject = request.Subject,
                Enquiry = request.Enquiry,
                EmailAccountId = request.EmailAccountId,
                Attributes = request.ContactAttributes,
                ContactAttributeDescription = request.ContactAttributeDescription,
                IpAddress = request.RemoteIpAddress
            };

            await _repository.InsertAsync(contactus);

            return true;
        }
    }
}
