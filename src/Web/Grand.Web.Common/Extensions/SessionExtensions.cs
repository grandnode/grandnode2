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
}