using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Grand.Web.Common.Extensions;

/// <summary>
///     Represents extensions of ISession
/// </summary>
public static class SessionExtensions
{
    /// <summary>
    ///     Set value to Session
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="session">Session</param>
    /// <param name="key">Key</param>
    /// <param name="value">Value</param>
    public static void Set<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    /// <summary>
    ///     Get value from session
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="session">Session</param>
    /// <param name="key">Key</param>
    /// <returns>Value</returns>
    public static T Get<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }

    /// <summary>
    ///     Wrong codes one password sign-in may try before it has to start again
    /// </summary>
    public const int MaxWrongTwoFactorCodes = 5;

    /// <summary>
    ///     Counts a wrong code for the sign-in waiting under the key; at the limit the waiting sign-in is dropped
    /// </summary>
    /// <param name="session">Session</param>
    /// <param name="key">Key that holds the sign-in waiting for its code</param>
    /// <returns>Whether the waiting sign-in was dropped</returns>
    public static bool CountWrongTwoFactorCode(this ISession session, string key)
    {
        var countKey = key + "WrongCodes";
        var count = (session.GetInt32(countKey) ?? 0) + 1;
        if (count < MaxWrongTwoFactorCodes)
        {
            session.SetInt32(countKey, count);
            return false;
        }

        session.Remove(key);
        session.Remove(countKey);
        return true;
    }

    /// <summary>
    ///     Starts the count of wrong codes again, for a new sign-in or one that has passed
    /// </summary>
    public static void ResetWrongTwoFactorCodes(this ISession session, string key)
    {
        session.Remove(key + "WrongCodes");
    }
}