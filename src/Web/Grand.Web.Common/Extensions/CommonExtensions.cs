using Grand.Web.Common.DataSource;

namespace Grand.Web.Common.Extensions;

/// <summary>
///     Extensions
/// </summary>
public static class CommonExtensions
{
    public static IEnumerable<T> PagedForCommand<T>(this IEnumerable<T> current, DataSourceRequest command)
    {
        return current.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize);
    }

    public static IEnumerable<T> PagedForCommand<T>(this IEnumerable<T> current, int pageIndex, int pageSize)
    {
        return current.Skip((pageIndex - 1) * pageSize).Take(pageSize);
    }
}