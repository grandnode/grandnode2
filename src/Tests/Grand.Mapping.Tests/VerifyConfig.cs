using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Grand.Mapping.Tests;

public static class VerifyConfig
{
    private static readonly Regex ObjectIdPattern = new(@"\b[0-9a-f]{24}\b", RegexOptions.Compiled);

    [ModuleInitializer]
    public static void Initialize()
    {
        // Replace auto-generated MongoDB ObjectIds with deterministic placeholders,
        // the same way Verify.ScrubInlineGuids() works for standard GUIDs.
        VerifierSettings.AddScrubber((sb) =>
        {
            var seen = new Dictionary<string, string>();
            var counter = 0;
            var result = ObjectIdPattern.Replace(sb.ToString(), m =>
            {
                if (!seen.TryGetValue(m.Value, out var label))
                {
                    label = $"ObjectId_{++counter}";
                    seen[m.Value] = label;
                }
                return label;
            });
            sb.Clear();
            sb.Append(result);
        });
    }
}
