using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public record GetCustomerData(Customer Customer) : IRequest<byte[]>;