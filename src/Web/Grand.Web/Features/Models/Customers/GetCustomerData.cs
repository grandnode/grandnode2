using Grand.Domain.Customers;
using MediatR;

namespace Grand.Web.Features.Models.Customers;

public record GetCustomerData(Customer Customer) : IRequest<byte[]>;