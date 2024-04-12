using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Grand.SharedKernel.Extensions;

/// <summary>
///     Represents a common helper
/// </summary>
public static class CommonHelper
{
    /// <summary>
    ///     Ensures the subscriber email or throw.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns></returns>
    public static string EnsureSubscriberEmailOrThrow(string email)
    {
        var output = EnsureNotNull(email);
        output = output.Trim();
        output = EnsureMaximumLength(output, 255);

        if (!IsValidEmail(output)) throw new GrandException("Email is not valid.");

        return output;
    }

    /// <summary>
    ///     Verifies that a string is in valid e-mail format
    /// </summary>
    /// <param name="email">Email to verify</param>
    /// <returns>true if the string is a valid e-mail address and false if it's not</returns>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        email = email.Trim();
        var result = Regex.IsMatch(email,
            "^(?:[\\w\\!\\#\\$\\%\\&\\'\\*\\+\\-\\/\\=\\?\\^\\`\\{\\|\\}\\~]+\\.)*[\\w\\!\\#\\$\\%\\&\\'\\*\\+\\-\\/\\=\\?\\^\\`\\{\\|\\}\\~]+@(?:(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\\-](?!\\.)){0,61}[a-zA-Z0-9]?\\.)+[a-zA-Z0-9](?:[a-zA-Z0-9\\-](?!$)){0,61}[a-zA-Z0-9]?)|(?:\\[(?:(?:[01]?\\d{1,2}|2[0-4]\\d|25[0-5])\\.){3}(?:[01]?\\d{1,2}|2[0-4]\\d|25[0-5])\\]))$",
            RegexOptions.IgnoreCase);
        return result;
    }

    /// <summary>
    ///     Generate random digit code
    /// </summary>
    /// <param name="length">Length</param>
    /// <returns>Result string</returns>
    public static string GenerateRandomDigitCode(int length)
    {
        var str = string.Empty;
        using var rng = RandomNumberGenerator.Create();
        var byteArray = new byte[length];
        rng.GetBytes(byteArray);
        for (var i = 0; i < length; i++)
            str = string.Concat(str, byteArray[i].ToString());

        return str[..length];
    }

    /// <summary>
    ///     Returns an random interger number within a specified rage
    /// </summary>
    /// <param name="min">Minimum number</param>
    /// <param name="max">Maximum number</param>
    /// <returns>Result</returns>
    public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue)
    {
        var randomNumberBuffer = new byte[10];
        RandomNumberGenerator.Create().GetBytes(randomNumberBuffer);
        return new Random(BitConverter.ToInt32(randomNumberBuffer, 0)).Next(min, max);
    }

    /// <summary>
    ///     Ensure that a string doesn't exceed maximum allowed length
    /// </summary>
    /// <param name="str">Input string</param>
    /// <param name="maxLength">Maximum length</param>
    /// <param name="postfix">A string to add to the end if the original string was shorten</param>
    /// <returns>Input string if its lengh is OK; otherwise, truncated input string</returns>
    public static string EnsureMaximumLength(string str, int maxLength, string postfix = null)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (str.Length > maxLength)
        {
            var pLen = postfix?.Length ?? 0;

            var result = str[..(maxLength - pLen)];
            if (string.IsNullOrEmpty(result))
                return str[..maxLength];

            if (!string.IsNullOrEmpty(postfix)) result += postfix;
            return result;
        }

        return str;
    }

    /// <summary>
    ///     Ensure that a string is not null
    /// </summary>
    /// <param name="str">Input string</param>
    /// <returns>Result</returns>
    public static string EnsureNotNull(string str)
    {
        return str ?? string.Empty;
    }

    /// <summary>
    ///     Compare two arrasy
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="a1">Array 1</param>
    /// <param name="a2">Array 2</param>
    /// <returns>Result</returns>
    public static bool ArraysEqual<T>(T[] a1, T[] a2)
    {
        if (ReferenceEquals(a1, a2))
            return true;

        if (a1 == null || a2 == null)
            return false;

        if (a1.Length != a2.Length)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < a1.Length; i++)
            if (!comparer.Equals(a1[i], a2[i]))
                return false;
        return true;
    }

    /// <summary>
    ///     Check if type is simple
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSimpleType(Type type)
    {
        if (type == null)
            return true;

        return
            type.IsPrimitive ||
            new[] {
                typeof(string),
                typeof(decimal),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Guid)
            }.Contains(type) ||
            type.IsEnum ||
            Convert.GetTypeCode(type) != TypeCode.Object ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
             IsSimpleType(type.GetGenericArguments()[0]))
            ;
    }

    /// <summary>
    ///     Converts a value to a destination type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="destinationType">The type to convert the value to.</param>
    /// <returns>The converted value.</returns>
    public static object To(object value, Type destinationType)
    {
        return To(value, destinationType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Converts a value to a destination type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="destinationType">The type to convert the value to.</param>
    /// <param name="culture">Culture</param>
    /// <returns>The converted value.</returns>
    public static object To(object value, Type destinationType, CultureInfo culture)
    {
        if (value != null)
        {
            var sourceType = value.GetType();

            var destinationConverter = TypeDescriptor.GetConverter(destinationType);
            if (destinationConverter.CanConvertFrom(value.GetType()))
                return destinationConverter.ConvertFrom(null, culture, value);

            var sourceConverter = TypeDescriptor.GetConverter(sourceType);
            if (sourceConverter.CanConvertTo(destinationType))
                return sourceConverter.ConvertTo(null, culture, value, destinationType);

            if (destinationType.IsEnum && value is int)
                return Enum.ToObject(destinationType, (int)value);

            if (!destinationType.IsInstanceOfType(value))
                return Convert.ChangeType(value, destinationType, culture);
        }

        return value;
    }

    /// <summary>
    ///     Converts a value to a destination type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <returns>The converted value.</returns>
    public static T To<T>(object value)
    {
        return (T)To(value, typeof(T));
    }

    /// <summary>
    ///     Convert enum for front-end
    /// </summary>
    /// <param name="value">Enum value</param>
    /// <returns>Converted string</returns>
    public static string ConvertEnum<T>(T value) where T : struct
    {
        var str = value.ToString();
        if (string.IsNullOrEmpty(str)) return string.Empty;
        var result = string.Empty;
        foreach (var c in str)
            if (c.ToString() != c.ToString().ToLower())
                result += " " + c;
            else
                result += c.ToString();
        return result.TrimStart();
    }


    /// <summary>
    ///     Get difference in years
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public static int GetDifferenceInYears(DateTime startDate, DateTime endDate)
    {
        //source: http://stackoverflow.com/questions/9/how-do-i-calculate-someones-age-in-c
        //this assumes you are looking for the western idea of age and not using East Asian reckoning.
        var age = endDate.Year - startDate.Year;
        if (startDate > endDate.AddYears(-age))
            age--;
        return age;
    }
}