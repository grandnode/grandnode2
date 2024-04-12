using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using MediatR;

namespace Grand.Business.Core.Queries.Catalog;

public record GetDiscountAmountProvider(
    Discount Discount,
    Customer Customer,
    Product Product,
    Currency Currency,
    double Amount) : IRequest<double>;