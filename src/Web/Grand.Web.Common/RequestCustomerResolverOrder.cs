namespace Grand.Web.Common;

/// <summary>
///     <see cref="IRequestCustomerResolver.Order" /> of the built-in resolvers, spaced so a plugin can slot in between.
/// </summary>
public static class RequestCustomerResolverOrder
{
    public const int AllowAnonymous = 1000;
    public const int Cookie = 2000;
    public const int GuestCookie = 3000;
    public const int SearchEngine = 4000;
    public const int ApiUser = 5000;
}
