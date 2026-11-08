namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Batch and serial numbers as people type or scan them
/// </summary>
public static class UnitNumbers
{
    private static readonly char[] Separators = ['\r', '\n', ',', ';'];

    /// <summary>
    ///     What two numbers are compared by: trimmed and upper-cased
    /// </summary>
    public static string Key(string number)
    {
        return number?.Trim().ToUpperInvariant() ?? "";
    }

    /// <summary>
    ///     The numbers of a text, one per line, or separated by commas or semicolons; trimmed, in order, empty ones left
    ///     out
    /// </summary>
    public static IList<string> Parse(string text)
    {
        return string.IsNullOrEmpty(text)
            ? []
            : text.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    /// <summary>
    ///     The numbers given more than once, by key, as first spelled
    /// </summary>
    public static IList<string> Duplicates(IEnumerable<string> numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        return numbers.GroupBy(Key).Where(x => x.Count() > 1).Select(x => x.First()).ToList();
    }
}
