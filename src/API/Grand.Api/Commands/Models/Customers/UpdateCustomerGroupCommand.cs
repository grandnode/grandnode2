using Grand.Api.DTOs.Customers;
using MediatR;

namespace Grand.Api.Commands.Models.Customers
{
    public class UpdateCustomerGroupCommand : IRequest<CustomerGroupDto>
    {
        public CustomerGroupDto Model { get; set; }
    }
}
