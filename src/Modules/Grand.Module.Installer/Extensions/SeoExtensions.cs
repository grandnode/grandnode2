namespace Grand.Module.Installer.Extensions;

public static class SeoExtensions
{
    /// <summary>
    ///     Get SE name
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="convertNonWesternChars">A value indicating whether non western chars should be converted</param>
    /// <param name="allowUnicodeCharsInUrls">A value indicating whether Unicode chars are allowed</param>
    /// <param name="allowSlashChar"></param>
    /// <param name="charConversions"></param>
    /// <returns>Result</returns>
    public static string GenerateSlug(string name, bool convertNonWesternChars = true, bool allowUnicodeCharsInUrls = false,
        bool allowSlashChar = false, string? charConversions = null)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var okChars = "abcdefghijklmnopqrstuvwxyz1234567890 _-";
        if (allowSlashChar)
            okChars += '/';

        name = name.Trim().ToLowerInvariant();

        if (convertNonWesternChars)
            if (!string.IsNullOrEmpty(charConversions) && _seoCharacterTable == null)
                InitializeSeoCharacterTable(charConversions);

        var sb = new StringBuilder();
        foreach (var c in name.ToCharArray())
        {
            var c2 = c.ToString();
            if (convertNonWesternChars && _seoCharacterTable != null)
                if (_seoCharacterTable.ContainsKey(c2))
                    c2 = _seoCharacterTable[c2];

            if (allowUnicodeCharsInUrls)
            {
                if (char.IsLetterOrDigit(c) || okChars.Contains(c2))
                    sb.Append(c2);
            }
            else if (okChars.Contains(c2))
            {
                sb.Append(c2);
            }
        }

        var name2 = sb.ToString();
        name2 = name2.Replace(" ", "-");
        while (name2.Contains("--"))
            name2 = name2.Replace("--", "-");
        while (name2.Contains("__"))
            name2 = name2.Replace("__", "_");
        return name2;
    }
    private static void InitializeSeoCharacterTable(string charConversions)
    {
        if (_seoCharacterTable != null) return;
        _seoCharacterTable = new Dictionary<string, string>();

        foreach (var conversion in charConversions.Split(";"))
        {
            var strLeft = conversion.Split(":").FirstOrDefault();
            var strRight = conversion.Split(":").LastOrDefault();
            if (!string.IsNullOrEmpty(strLeft) && !_seoCharacterTable.ContainsKey(strLeft))
                _seoCharacterTable.Add(strLeft.Trim(), strRight!.Trim());
        }
    }
    private static Dictionary<string, string>? _seoCharacterTable;
}
