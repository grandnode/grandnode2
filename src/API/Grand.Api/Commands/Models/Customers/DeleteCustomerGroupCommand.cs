using Grand.Api.DTOs.Customers;
using MediatR;

namespace Grand.Api.Commands.Models.Customers
{
    public class DeleteCustomerGroupCommand : IRequest<bool>
    {
        public CustomerGroupDto Model { get; set; }
    }
}
