using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common;

/// <summary>
///     Decides the customer of a request from a credential the request carries (an access token, a key...).
///     Registered in DI; the work context asks every resolver in <see cref="Order" /> and takes the first
///     non-null customer. Return null when the request is not yours; return a customer (for example the
///     Anonymous system customer) to stop the chain when you recognise the credential but reject it.
/// </summary>
public interface IRequestCustomerResolver
{
    int Order { get; }

    Task<Customer> Resolve(HttpContext httpContext, Store store);
}
