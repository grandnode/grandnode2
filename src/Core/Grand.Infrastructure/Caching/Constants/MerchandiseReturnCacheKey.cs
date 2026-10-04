namespace Grand.Infrastructure.Caching.Constants;

public static partial class CacheKey
{
    /// <summary>
    ///     Key for all merchandise return reasons. {0} - store ID
    /// </summary>
    public static string MERCHANDISE_RETURN_REASONS_ALL_KEY => "Grand.merchandisereturn.reasons.all-{0}";

    /// <summary>
    ///     Key pattern to clear merchandise return reasons cache
    /// </summary>
    public static string MERCHANDISE_RETURN_REASONS_PATTERN_KEY => "Grand.merchandisereturn.reasons";

    /// <summary>
    ///     Key for all merchandise return actions. {0} - store ID
    /// </summary>
    public static string MERCHANDISE_RETURN_ACTIONS_ALL_KEY => "Grand.merchandisereturn.actions.all-{0}";

    /// <summary>
    ///     Key pattern to clear merchandise return actions cache
    /// </summary>
    public static string MERCHANDISE_RETURN_ACTIONS_PATTERN_KEY => "Grand.merchandisereturn.actions";
}