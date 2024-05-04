using DotLiquid;
using DotLiquid.NamingConventions;
using Grand.Domain.Messages;
using System.Reflection;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public static class LiquidExtensions
{
    public static List<string> GetTokens(params Type[] drops)
    {
        var toReturn = new List<string>();
        foreach (var drop in drops)
            toReturn.AddRange(drop
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(x => "{{" + drop.Name[6..] + "." + x.Name + "}}"));

        return toReturn;
    }

    public static string Render(LiquidObject liquidObject, string source)
    {
        var hash = Hash.FromAnonymousObject(liquidObject);
        Template.NamingConvention = new CSharpNamingConvention();
        var template = Template.Parse(source);
        var replaced = template.Render(hash);

        return replaced;
    }
}