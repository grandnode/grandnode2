using MediatR;

namespace Grand.Business.Marketing.Commands.Models
{
    public class UpdateCustomerReminderHistoryCommand : IRequest<bool>
    {
        public string CustomerId { get; set; }
        public string OrderId { get; set; }
    }
}
