using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Web.Common;

/// <summary>
///     One step in deciding the customer of a request. Registered in DI (scoped); the work context asks every
///     resolver in ascending <see cref="Order" /> and the first non-null customer wins. Resolvers take what they
///     need (the current request included) by constructor injection.
/// </summary>
/// <remarks>
///     The built-in steps are registered with the values of <see cref="RequestCustomerResolverOrder" />; pick a
///     value between them to run before or after a built-in step. A resolver that throws fails the request.
/// </remarks>
public interface IRequestCustomerResolver
{
    /// <summary>
    ///     Position in the chain, ascending. Built-in steps use <see cref="RequestCustomerResolverOrder" />.
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     True only for the signed-in (cookie) identity: the work context then applies admin impersonation
    ///     to the customer this resolver returns.
    /// </summary>
    bool SupportsImpersonation => false;

    /// <summary>
    ///     Decide the customer of the current request.
    /// </summary>
    /// <param name="store">The store of the request</param>
    /// <returns>
    ///     null when the request is not this resolver's, so the next resolver is asked. A customer stops the chain
    ///     (for example the Anonymous system customer when the credential is recognised but rejected).
    /// </returns>
    Task<Customer> Resolve(Store store);
}
