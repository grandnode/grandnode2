using DotLiquid;
using DotLiquid.NamingConventions;
using Grand.Domain.Messages;
using System.Net;
using System.Reflection;

namespace Grand.Business.Messages.DotLiquidDrops;

public static class LiquidExtensions
{
    public static List<string> GetTokens(params ReadOnlySpan<Type> drops)
    {
        List<string> tokens = [];
        foreach (var drop in drops)
            tokens.AddRange(drop
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(property => "{{" + drop.Name[6..] + "." + property.Name + "}}"));

        return tokens;
    }

    public static string Render(LiquidObject liquidObject, string source)
    {
        var hash = Hash.FromAnonymousObject(liquidObject);
        Template.NamingConvention = new CSharpNamingConvention();
        var template = Template.Parse(source);
        var replaced = template.Render(hash);

        return replaced;
    }

    /// <summary>
    ///     Renders a subject line: tokens are HTML-encoded for the body, and a subject is plain text
    /// </summary>
    public static string RenderSubject(LiquidObject liquidObject, string source)
    {
        return WebUtility.HtmlDecode(Render(liquidObject, source));
    }
}