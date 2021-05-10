using Grand.Domain.Common;
using MediatR;
using System.Collections.Generic;

namespace Grand.Business.Messages.Commands.Models
{
    public class InsertContactUsCommand : IRequest<bool>
    {
        public string CustomerId { get; set; }
        public string StoreId { get; set; }
        public string VendorId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Subject { get; set; }
        public string Enquiry { get; set; }
        public string ContactAttributeDescription { get; set; }
        public IList<CustomAttribute> ContactAttributes { get; set; }
        public string EmailAccountId { get; set; }
    }

}
