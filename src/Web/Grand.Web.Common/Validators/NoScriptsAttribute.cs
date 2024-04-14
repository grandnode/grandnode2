using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Grand.Web.Common.Validators;

public class NoScriptsAttribute : ValidationAttribute
{
    // Simple regular expression to detect potential scripts
    private const string Pattern =
        "<script.*?>.*?</script>|javascript:[^\\s]*|onload=|onerror=|onmouseover=|onclick=|onchange=|onsubmit=";

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;
        var valueAsString = value.ToString();
        // Check if the value contains a script
        return ContainsScript(valueAsString)
            ? new ValidationResult("JavaScript scripts are not allowed.")
            : ValidationResult.Success;
    }

    private static bool ContainsScript(string input)
    {
        var scriptRegex = new Regex(Pattern, RegexOptions.IgnoreCase);
        return scriptRegex.IsMatch(input);
    }
}