using Grand.Business.Messages.Commands.Models;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Commands.Handlers
{
    public class InsertContactUsCommandHandler : IRequestHandler<InsertContactUsCommand, bool>
    {
        private readonly IRepository<ContactUs> _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public InsertContactUsCommandHandler(
            IRepository<ContactUs> repository,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> Handle(InsertContactUsCommand request, CancellationToken cancellationToken)
        {
            var contactus = new ContactUs()
            {
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
                IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
            };

            await _repository.InsertAsync(contactus);

            return true;
        }
    }
}
