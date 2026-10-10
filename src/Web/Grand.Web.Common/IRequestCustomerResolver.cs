using Grand.Domain.Customers;

namespace Grand.Web.Common;

/// <summary>
///     One step in deciding the customer of a request. Registered in DI (scoped); the work context asks every
///     resolver in ascending <see cref="Order" /> and the first non-null customer wins. Resolvers take what they
///     need (the current request and store included) by constructor injection.
/// </summary>
/// <remarks>
///     The built-in steps are registered with the values of <see cref="RequestCustomerResolverOrder" />; pick a
///     value between them to run before or after a built-in step. Resolvers are created only for requests, never
///     in a background task scope. A resolver whose <see cref="Resolve" /> throws is logged and skipped. Admin
///     impersonation applies only to the signed-in (cookie) customer, never to a customer another resolver returns.
/// </remarks>
public interface IRequestCustomerResolver
{
    /// <summary>
    ///     Position in the chain, ascending. Built-in steps use <see cref="RequestCustomerResolverOrder" />.
    ///     Equal values run in registration order; pick a value no other resolver uses.
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     Decide the customer of the current request.
    /// </summary>
    /// <returns>
    ///     null when the request is not this resolver's, so the next resolver is asked. A customer stops the chain
    ///     (for example the Anonymous system customer when the credential is recognised but rejected).
    /// </returns>
    Task<Customer> Resolve();
}
